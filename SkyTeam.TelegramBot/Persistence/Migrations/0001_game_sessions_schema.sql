CREATE TABLE IF NOT EXISTS GameSessions (
    GameId TEXT NOT NULL PRIMARY KEY,
    GroupChatId INTEGER NOT NULL,
    PilotUserId INTEGER NOT NULL,
    CopilotUserId INTEGER NOT NULL,
    StateJson TEXT NOT NULL,
    Status TEXT NOT NULL,
    Version INTEGER NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL,
    ExpiresAtUtc TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_GameSessions_GroupChatId
    ON GameSessions (GroupChatId);

CREATE UNIQUE INDEX IF NOT EXISTS UX_GameSessions_Active_GroupChatId
    ON GameSessions (GroupChatId)
    WHERE Status IN ('AwaitingRoll', 'AwaitingPlacements', 'ReadyToResolve');
