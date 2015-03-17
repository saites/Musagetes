delete from Song;
insert into Song (songtitle, primaryartist, seconds, location, bpm, bpmguess) values ('Mud on the Tires', 'Brad Paisley', 200, 'E:\Music\Brad Paisley\Brad Paisley - Mud on The Tires\Brad Paisley - 01 - Mud On The Tires.mp3', 140, 0);

/*insert into tag ([tagname]) values ('Country');*/

/*
insert into SongTag ([SongId], [TagId]) values ((select songid from song where SongTitle = 'Crash My Party'), (select tagid from tag where TagName = 'Country'));
*/