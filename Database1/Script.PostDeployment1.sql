/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
-- Use the newly created database
USE Games;
GO

-- Create Games table
CREATE TABLE Games (
    GameID INT PRIMARY KEY IDENTITY(1,1),
    GameName NVARCHAR(100) NOT NULL,
    BackgroundImage NVARCHAR(MAX),
    [desc] NVARCHAR(MAX),
	CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);
GO
CREATE TABLE GamesVariant (
    ID INT PRIMARY KEY IDENTITY(1,1),
    GameID INT,
    name NVARCHAR(100) NOT NULL,
    [desc] NVARCHAR(MAX),
    Levels NVARCHAR(MAX),    
	FOREIGN KEY (GameID) REFERENCES Games(GameID)
);
GO
create table config(
    id int PRIMARY KEY IDENTITY(1,1),
	[key] varchar(100),
	value varchar(200),
	gameid int,
	isactive bit,
	CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);



-- Create Players table
CREATE TABLE Players (
    PlayerID INT PRIMARY KEY IDENTITY(1,1),
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    DateOfBirth DATE,
	email varchar(100),	
  	CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- Create Bookings table
CREATE TABLE Bookings (
    BookingID INT PRIMARY KEY IDENTITY(1,1),
    BookingDate DATETIME NOT NULL DEFAULT GETDATE(),
	bookingPlayerId int,
	email varchar(100), 
    SessionStartDate Datetime, -- Duration in minutes
	SessionEndDate Datetime,
    GameID INT,
   CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);

GO

-- Create PlayerScores table
CREATE TABLE PlayerScores (
    ScoreID INT PRIMARY KEY IDENTITY(1,1),
    PlayerID INT,
    GameID INT,
	BookingID int,
    LevelPlayed NVARCHAR(100),
    Points INT NOT NULL,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NOT NULL,
	CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (PlayerID) REFERENCES Players(PlayerID),
    FOREIGN KEY (GameID) REFERENCES Games(GameID),
	FOREIGN KEY (BookingID) REFERENCES Bookings(BookingID)
);
GO

