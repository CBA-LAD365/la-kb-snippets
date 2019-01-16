#import <UIKit/UIKit.h>
#import <LiveAssist/LiveAssist.h>

//{Auth Chat}
@interface TabBarViewController : UITabBarController<LiveAssistDelegate>

@property LiveAssistView *assistView;

@end
