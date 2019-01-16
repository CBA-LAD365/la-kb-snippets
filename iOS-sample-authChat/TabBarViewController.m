#import "TabBarViewController.h"
#import <LiveAssist/LiveAssist.h>
@import WebKit;

@implementation TabBarViewController

- (void)viewDidLoad {
    [super viewDidLoad];
    [self setupLiveAssist];
}

//{Auth Chat}
-(void)
authoriseChatWithCallback : (AuthoriseCallback) callback {
    NSString *authString = @"PutJWTHere";  callback(authString);
}


-(void) setupLiveAssist {
    AssistConfig* assistConfig = [self assistConfig];
    
    //{Auth Chat}
    assistConfig.javascriptMethodName=@"authoriseChatWithCallback";
    assistConfig.delegate=self;
    
    assistConfig.mask = [self assistMask];
    _assistView = [[LiveAssistView alloc] initWithAssistConfig:assistConfig];
    [self.view addSubview:_assistView];
    [_assistView setSections:@[@"testme"]];
    
}

-(AssistConfig*) assistConfig {
    NSInteger accountId = [[[NSBundle mainBundle] objectForInfoDictionaryKey:@"AccountId"] integerValue];
    LiveAssistChatStyle style = LiveAssistChatStyleFullScreen;
    BOOL notifications = YES;
    NSArray *sections = @[@""];
    
    return [AssistConfig assistConfigWithAccountId:accountId sections:sections chatStyle:style frame:self.view.frame notifications:notifications];
}

-(AssistMask*) assistMask {
    NSSet* sets = [NSSet setWithArray:@[@1,@2]];
    return [AssistMask withTagSet:sets andColor:[UIColor blackColor]];
}


-(void) showNoAccountIdAlert {
    UIAlertController * alert=   [UIAlertController
                                  alertControllerWithTitle:@"Info"
                                  message:@"Please set your account identifer in the plist"
                                  preferredStyle:UIAlertControllerStyleAlert];

    UIAlertAction* defaultAction = [UIAlertAction actionWithTitle:@"OK" style:UIAlertActionStyleDefault
                                                          handler:^(UIAlertAction * action) {}];
    
    [alert addAction:defaultAction];
    
    [self presentViewController:alert animated:NO completion:nil];
}

-(void) viewDidLayoutSubviews {
    if([[NSBundle mainBundle] objectForInfoDictionaryKey:@"AccountId"] == nil){
        [self showNoAccountIdAlert];
        
    }
    

}

@end
