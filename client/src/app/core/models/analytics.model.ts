export interface AnalyticsOverviewDto {
  total_members: number;
  active_members: number;
  new_members_this_month: number;
  total_collected: number;
  pending_amount: number;
  total_events: number;
  upcoming_events: number;
  total_meetings: number;
  total_elections: number;
}

export interface MonthlySeriesPoint {
  month: string;
  value: number;
}

export interface MembersByStatusDto {
  status: string;
  count: number;
}

export interface MemberAnalyticsDto {
  monthly_growth: MonthlySeriesPoint[];
  by_status: MembersByStatusDto[];
}

export interface FinanceAnalyticsDto {
  monthly_collected: MonthlySeriesPoint[];
  monthly_expected: MonthlySeriesPoint[];
  total_collected: number;
  total_expected: number;
  collection_rate: number;
}

export interface EngagementAnalyticsDto {
  meeting_attendance_rate: number;
  election_participation_rate: number;
  total_event_registrations: number;
}
