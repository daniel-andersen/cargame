//
//  Client.h
//  Car Game
//
//  Created by Daniel Andersen on 17/01/14.
//  Copyright (c) 2014 Alexandra Instituttet. All rights reserved.
//

#import <Foundation/Foundation.h>

@protocol ClientDelegate <NSObject>

- (void)connectionOpened;
- (void)connectionClosed;
- (void)connectionError:(NSString *)message;

- (void)receivedMessage:(NSString *)message;

@end

@interface Client : NSObject <NSStreamDelegate>

- (void)initConnection;
- (void)closeConnection;
- (void)sendMessage:(NSString *)message;

@property (nonatomic, retain) id<ClientDelegate> delegate;

@end
