export enum ReportStatus {
  Submitted = 0,
  InProgress = 1,
  Resolved = 2,
  Closed = 3
}

export const StatusBadge = {
  [ReportStatus.Submitted]: 'bg-warning text-dark',
  [ReportStatus.InProgress]: 'bg-info text-white',
  [ReportStatus.Resolved]: 'bg-success text-white',
  [ReportStatus.Closed]: 'bg-secondary text-white'
};

export const statusLabels: Record<ReportStatus, string> = {
  [ReportStatus.Submitted]: 'Submitted',
  [ReportStatus.InProgress]: 'In Progress',
  [ReportStatus.Resolved]: 'Resolved',
  [ReportStatus.Closed]: 'Closed'
};

export interface ReportPhoto {
  id: number;
  photoUrl: string;
  caption?: string;
  isPrimary: boolean;
}

export interface Report {
  id: number;
  title: string;
  description: string;
  category: string;
  locationAddress: string;
  status: ReportStatus;
  priority: number;
  upvoteCount: number;
  createdAt: string;
  createdByUserId: number;
  assignedToUserId?: number | null;
  photos: ReportPhoto[];
}

// THIS WAS MISSING — ADD IT!
export interface CreateReportRequest {
  title: string;
  description: string;
  category: string;
  locationAddress: string;
}