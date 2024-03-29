CREATE TABLE "StatusEntry" (
	"Id"	INTEGER PRIMARY KEY AUTOINCREMENT,
	"Nation"	INTEGER NOT NULL,
	"Status"	INTEGER NOT NULL,
	"Additional" TEXT,
	"CreatedAt"	INTEGER NOT NULL,
	"DisabledAt"	INTEGER,
);
CREATE TABLE "CommandUsage" (
	"Id"	INTEGER PRIMARY KEY AUTOINCREMENT,
	"TraceId"	TEXT,
	"Timestamp"	INTEGER NOT NULL,
	"UserId"	INTEGER NOT NULL,
	"ChannelId"	INTEGER NOT NULL,
	"IsPrimaryGuild"	BOOLEAN NOT NULL,
	"IsDM"	BOOLEAN NOT NULL,
	"GuildId"	INTEGER,
	"CommandType"	INTEGER NOT NULL,
	"Command"	TEXT NOT NULL,
	"CompleteTime"	NUMERIC
);