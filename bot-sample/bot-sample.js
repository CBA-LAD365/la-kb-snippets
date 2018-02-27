"use strict";
var builder = require("botbuilder");
var botbuilder_azure = require("botbuilder-azure");
var path = require('path');

const TRANSFER_MESSAGE = 'transfer to ';

var useEmulator = (process.env.NODE_ENV == 'development');

var connector = useEmulator ? new builder.ChatConnector() : new botbuilder_azure.BotServiceConnector({
    appId: process.env['MicrosoftAppId'],
    appPassword: process.env['MicrosoftAppPassword'],
    stateEndpoint: process.env['BotStateEndpoint'],
    openIdMetadata: process.env['BotOpenIdMetadata']
});

var bot = new builder.UniversalBot(connector);
bot.localePath(path.join(__dirname, './locale'));

bot.dialog('/', function (session) {
    
    switch(session.message.sourceEvent.type)
    {
        case "visitorContextData":
            //process context data if required. This is the first message received so say hello.
            session.send('Hi, I am an echo bot and will repeat everything you said.');
            break;

        case "systemMessage":
            //react to system messages if required
            break;

        case "transferFailed":
            //react to transfer failures if required
            break;

        case "otherAgentMessage":
            //react to messages from a supervisor if required
            break;

        case "visitorMessage":
            // Check for transfer message
			if(session.message.text.startsWith(TRANSFER_MESSAGE)){
				var transferTo = session.message.text.substr(TRANSFER_MESSAGE.length);
				var msg = new builder.Message(session).sourceEvent({directline: {type: "transfer", agent: transferTo}});
				session.send(msg);
			}else {
				session.send('You said ' + session.message.text);
			}
            break;

        default:
            session.send('This is not a Live Assist message ' + session.message.sourceEvent.type);
    }
    
});

if (useEmulator) {
    var restify = require('restify');
    var server = restify.createServer();
    server.listen(3978, function() {
        console.log('test bot endpont at http://localhost:3978/api/messages');
    });
    server.post('/api/messages', connector.listen());    
} else {
    module.exports = { default: connector.listen() }
}
