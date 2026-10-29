-- Run with the module disabled and all API/background writers stopped.
-- Any row returned requires a traced business decision before migration.
SELECT tenant_id, requester_member_id, COUNT(*) AS open_request_count,
       GROUP_CONCAT(id ORDER BY created_at, id) AS request_ids
FROM transfer_assistance_requests
WHERE is_active = 1 AND status IN (
    'draft', 'submitted', 'matching', 'witness_pending',
    'meeting_scheduled', 'transfer_in_progress', 'transfer_sent',
    'disputed', 'suspended'
)
GROUP BY tenant_id, requester_member_id
HAVING COUNT(*) > 1;

SELECT VERSION() AS mysql_version, @@SESSION.sql_mode AS sql_mode,
       @@character_set_database AS character_set_database,
       @@collation_database AS collation_database,
       @@SESSION.time_zone AS session_time_zone, @@system_time_zone AS system_time_zone;
