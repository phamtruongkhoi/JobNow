CREATE TABLE jn_conversations (
    id SERIAL PRIMARY KEY,
    candidate_profile_id text NOT NULL,
    employer_profile_id text NOT NULL,
    job_id integer NOT NULL,
    job_title text,
    last_message text,
    last_message_at timestamp,
    created_at timestamp DEFAULT now(),
    UNIQUE(candidate_profile_id, employer_profile_id, job_id)
);

CREATE TABLE jn_messages (
    id SERIAL PRIMARY KEY,
    conversation_id integer NOT NULL REFERENCES jn_conversations(id) ON DELETE CASCADE,
    sender_profile_id text NOT NULL,
    message text NOT NULL,
    is_read boolean DEFAULT false,
    created_at timestamp DEFAULT now()
);

ALTER TABLE jn_conversations DISABLE ROW LEVEL SECURITY;
ALTER TABLE jn_messages DISABLE ROW LEVEL SECURITY;
