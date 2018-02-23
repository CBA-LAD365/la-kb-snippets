const liveAssist = require('@cafex/liveassist-botsdk-js');
var restify = require('restify');
var builder = require('botbuilder');

// Setup Restify Server
var server = restify.createServer();
server.listen(process.env.port || process.env.PORT || 3978, function() {
 console.log('%s listening to %s', server.name, server.url);
});

// Create chat connector for communicating with the Bot Framework Service
var connector = new builder.ChatConnector({
 appId: process.env.MICROSOFT_APP_ID,
 appPassword: process.env.MICROSOFT_APP_PASSWORD
});

// Listen for messages from users
server.post('/api/messages', connector.listen());

// Receive messages from the user and respond by echoing each message back (prefixed with 'You said:')
var bot = new builder.UniversalBot(connector);

const MODE = {
 BOT: 0,
 ESC_INITIATED: 1,
 ESC_WAITING: 2,
 ESC_CHATTING: 3,
};

let chatData;

function removeChatData() {
 chatData = undefined;
}

function getChatData(session) {
 if (!chatData) {
   chatData = {
     visitorAddress: session.message.address,
     mode: MODE.BOT
   };
 }
 return chatData;
}

bot.dialog('/',
 function(session) {
   let chatData = getChatData(session);
   switch (chatData.mode) {
     case MODE.BOT:
       if (/^help/i.test(session.message.text)) {
         session.beginDialog('/escalateQuery');
       } else {
         let visitorText = session.message.text;
         let botText = 'You said: "' + visitorText + '"';
         session.send(botText, session.message.text);
       }
       break;
     case MODE.ESC_INITIATED:
       session.send('Please wait, I\'m trying to connect you to an agent');
       break;
     case MODE.ESC_WAITING:
       session.send('Please wait, waiting for an agent');
       break;
     case MODE.ESC_CHATTING:
       if (/^stop/i.test(session.message.text)) {
         chatData.escalatedChat.endChat((err) => {
           if (err) {
             session.endConversation('A problem has occurred, starting over');
             removeChatData();
           } else {
             chatData.mode = MODE.BOT;
           }
         });
       } else {
         chatData.escalatedChat.addLine(session.message.text, (err) => {
           if (err) {
             session.send('A problem has occurred sending that');
           }
         });
       }
       break;
     default:
   }
 }
);

bot.dialog('/escalateQuery', [
 function(session) {
   session.send('Please wait while I connect you to an agent');
   let spec = {
     skill: '-YOUR SKILL-',
   };
   chatData.escalatedChat = new liveAssist.Chat('-YOUR ACCOUNT ID-');
   chatData.escalatedChat.requestChat(spec, (err) => {
     if (err) {
       session.send('Sorry, I failed to contact an agent');
       chatData.mode = MODE.BOT;
     } else {
       chatData.mode = MODE.ESC_INITIATED;
       pollChat();
     }
   });
   session.endDialog();
 }
]);

function pollChat() {
 chatData.escalatedChat.poll((err, result) => {
   let endRead = false;
   if (err) {
     console.error('Error during poll: %s', err.message);
   } else {
     endRead = processEvents(result.events);
   }
   if (!endRead) setTimeout(() => pollChat(), 500);
 });
}

function processEvents(events) {
 let endRead = false;
 events.forEach((event) => {
   switch (event.type) {
     case 'state':
       switch (event.state) {
         case 'waiting':
           chatData.mode = MODE.ESC_WAITING;
           break;
         case 'chatting':
           chatData.mode = MODE.ESC_CHATTING;
           break;
         case 'ended':
           endRead = true;
           bot.beginDialog(chatData.visitorAddress, '*:/endit', chatData);
           break;
         default:
           break;
       }
       break;
     case 'line':
       if (event.source !== 'visitor') {
         let msg = event.text;
         sendProactiveMessage(chatData.visitorAddress, msg);
       }
       break;
     default:
       break;
   }
 });
 return endRead;
}

function sendProactiveMessage(address, text) {
 var msg = new builder.Message().address(address);
 msg.text(text);
 bot.send(msg);
}

bot.dialog('/endit', [
 function(session) {
   session.endConversation('Agent interaction has ended');
   removeChatData();
 }
]);