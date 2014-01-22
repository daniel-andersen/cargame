//
//  ViewController.m
//  Car Game
//
//  Created by Daniel Andersen on 17/01/14.
//  Copyright (c) 2014 Alexandra Instituttet. All rights reserved.
//

#import <CoreMotion/CoreMotion.h>

#import "ViewController.h"

#define BACKWARDS_DIAMETER (M_PI / 8.0f)

@interface ViewController () {
    Client *client;
    NSString *clientId;
    CMMotionManager *motionManager;
    bool joined;
    NSTimer *connectionPollTimer;
}

@end

@implementation ViewController

- (void)viewDidLoad {
    [super viewDidLoad];

    client = nil;
    joined = NO;
    clientId = [self uniqueId];
    
    motionManager = [[CMMotionManager alloc] init];
    [motionManager startDeviceMotionUpdates];
    
    connectionPollTimer = [NSTimer scheduledTimerWithTimeInterval:2.0f target:self selector:@selector(connectionPoll) userInfo:nil repeats:YES];
}

- (void)connectionPoll {
    NSLog(@"Connection poll...");
    if (client == nil) {
        client = [[Client alloc] init];
        client.delegate = self;
    }
    [client initConnection];
}

- (void)didReceiveMemoryWarning {
    [super didReceiveMemoryWarning];
}

- (void)viewDidAppear:(BOOL)animated {
}

- (void)connectionOpened {
    NSLog(@"Connection opened!");
    [connectionPollTimer invalidate];
    connectionPollTimer = nil;
    [client sendMessage:[NSString stringWithFormat:@"CONNECT|%@", clientId]];
}

- (void)connectionClosed {
    NSLog(@"Connection closed!");
    [self showExitDialogBoxWithTitle:@"Connection closed" message:@"Connection to server closed. Exiting..."];
}

- (void)connectionError:(NSString *)message {
    if (joined) {
        [self showExitDialogBoxWithTitle:@"Connection closed" message:@"Connection to server closed. Exiting..."];
    }
}

- (void)receivedMessage:(NSString *)message {
    message = [[message stringByReplacingOccurrencesOfString:@"\n" withString:@""] stringByReplacingOccurrencesOfString:@"\r" withString:@""];
    NSArray *messageArray = [message componentsSeparatedByString:@"|"];

    if ([@"JOIN" isEqualToString:[messageArray objectAtIndex:0]]) {
        [self joined];
    }
    if ([@"READY" isEqualToString:[messageArray objectAtIndex:0]]) {
        [self sendCoords];
    }
}

- (void)joined {
    joined = YES;
    self.view.backgroundColor = [UIColor whiteColor];
    [UIView animateWithDuration:1.0f animations:^{
        self.view.backgroundColor = [UIColor blackColor];
    }];
    [self sendCoords];
}

- (void)sendCoords {
    [client sendMessage:[NSString stringWithFormat:@"UPDATE|%f|%f|%f", [self calculateSteeringAngle], [self calculateThrottle], [self calculateBrake]]];
}

- (float)calculateSteeringAngle {
    return motionManager.deviceMotion.attitude.pitch * 0.8f;
}

- (float)calculateThrottle {
    if (motionManager.deviceMotion.attitude.roll >= -M_PI / 2.0f && motionManager.deviceMotion.attitude.roll < 0.0f) {
        return 100.0f * ((M_PI / 2.0f) - (-motionManager.deviceMotion.attitude.roll));
    } else if (motionManager.deviceMotion.attitude.roll >= -M_PI && motionManager.deviceMotion.attitude.roll < -(M_PI / 2.0f) - BACKWARDS_DIAMETER) {
        return -50.0f;
    } else {
        return 0.0f;
    }
}

- (float)calculateBrake {
    return 0.0f;
}

- (NSString *)uniqueId {
    return [NSString stringWithFormat:@"%d", arc4random()];
}

- (void)showExitDialogBoxWithTitle:(NSString *)title message:(NSString *)message {
    [[[UIAlertView alloc] initWithTitle:title message:message delegate:self cancelButtonTitle:@"OK" otherButtonTitles:nil] show];
}

- (void)alertView:(UIAlertView *)alertView clickedButtonAtIndex:(NSInteger)buttonIndex {
    exit(0);
}

@end
