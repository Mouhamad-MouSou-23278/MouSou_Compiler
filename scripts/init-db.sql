-- ==============================================================================
-- Online Compiler V2 - Database Initialization Script
-- ==============================================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Create indexes for performance
-- (EF Core handles schema generation; this script ensures DB extensions and roles exist)
GRANT ALL PRIVILEGES ON DATABASE online_compiler TO postgres;
