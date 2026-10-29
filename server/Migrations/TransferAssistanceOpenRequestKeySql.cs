namespace server.Migrations;

internal static class TransferAssistanceOpenRequestKeySql
{
    // Duplicate keys stop the migration before persistent changes; the key identifies the group to resolve.
    internal const string Preflight = """
        DROP TEMPORARY TABLE IF EXISTS transfer_assistance_open_request_preflight;
        CREATE TEMPORARY TABLE transfer_assistance_open_request_preflight (
            tenant_requester_key varchar(65) CHARACTER SET ascii COLLATE ascii_general_ci PRIMARY KEY
        );
        INSERT INTO transfer_assistance_open_request_preflight (tenant_requester_key)
        SELECT CONCAT(
            REPLACE(LOWER(CAST(tenant_id AS CHAR)), '-', ''),
            ':',
            REPLACE(LOWER(CAST(requester_member_id AS CHAR)), '-', '')
        )
        FROM transfer_assistance_requests
        WHERE is_active = 1 AND status IN (
            'draft', 'submitted', 'matching', 'witness_pending', 'intermediary_assigned',
            'meeting_scheduled', 'transfer_in_progress', 'transfer_sent',
            'disputed', 'suspended'
        );
        DROP TEMPORARY TABLE transfer_assistance_open_request_preflight;
        """;

    internal const string Backfill = """
        UPDATE transfer_assistance_requests
        SET open_request_key = NULL
        WHERE is_active = 0 OR status NOT IN (
            'draft', 'submitted', 'matching', 'witness_pending', 'intermediary_assigned',
            'meeting_scheduled', 'transfer_in_progress', 'transfer_sent',
            'disputed', 'suspended'
        );
        UPDATE transfer_assistance_requests
        SET open_request_key = CONCAT(
            REPLACE(LOWER(CAST(tenant_id AS CHAR)), '-', ''),
            ':',
            REPLACE(LOWER(CAST(requester_member_id AS CHAR)), '-', '')
        )
        WHERE is_active = 1 AND status IN (
            'draft', 'submitted', 'matching', 'witness_pending', 'intermediary_assigned',
            'meeting_scheduled', 'transfer_in_progress', 'transfer_sent',
            'disputed', 'suspended'
        );
        """;
}
