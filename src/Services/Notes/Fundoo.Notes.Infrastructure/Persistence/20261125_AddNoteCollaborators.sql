CREATE TABLE IF NOT EXISTS note_collaborators
(
    note_id UUID NOT NULL REFERENCES notes(id) ON DELETE CASCADE,
    user_id UUID NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (note_id, user_id)
);

CREATE INDEX IF NOT EXISTS ix_note_collaborators_user_id
    ON note_collaborators (user_id, note_id);
