//
//  ViewController.m
//  Car Game
//
//  Created by Daniel Andersen on 17/01/14.
//  Copyright (c) 2014 Alexandra Instituttet. All rights reserved.
//

#import "EnterDetailsViewController.h"
#import "GameViewController.h"

@interface EnterDetailsViewController ()

@property (strong, nonatomic) IBOutlet UITextField *ipAddressTextField;

@end

@implementation EnterDetailsViewController

- (void)viewDidLoad {
    [super viewDidLoad];
    self.ipAddressTextField.delegate = self;
}

- (void)didReceiveMemoryWarning {
    [super didReceiveMemoryWarning];
}

- (void)viewDidAppear:(BOOL)animated {
}

- (BOOL)textFieldShouldReturn:(UITextField *)textField {
    [self.ipAddressTextField resignFirstResponder];
    return YES;
}

- (void)prepareForSegue:(UIStoryboardSegue *)segue sender:(id)sender {
    if ([@"enterDetailsToGameSegue" isEqualToString:segue.identifier]) {
        GameViewController *gameViewController = (GameViewController *)segue.destinationViewController;
        gameViewController.ipAddress = self.ipAddressTextField.text;
    }
}

@end
