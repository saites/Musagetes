CREATE TABLE [dbo].[Song]
(
	[SongId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [SongTitle] VARCHAR(MAX) NOT NULL, 
    [PrimaryArtist] VARCHAR(MAX) NULL, 
    [Seconds] INT NOT NULL
)
