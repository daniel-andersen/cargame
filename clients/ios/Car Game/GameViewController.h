//
//  GameViewController.h
//  Car Game
//
//  Created by Daniel Andersen on 27/01/14.
//  Copyright (c) 2014 Alexandra Instituttet. All rights reserved.
//

#import <UIKit/UIKit.h>

#import "Client.h"

@interface GameViewController : UIViewController <ClientDelegate, UIAlertViewDelegate>

@property (nonatomic, retain) NSString *ipAddress;

@end
