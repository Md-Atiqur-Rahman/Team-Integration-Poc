export interface TeamItem {
  id: string;
  displayName: string | null;
  description: string | null;
  isArchived: boolean | null;
}

export interface ChannelItem {
  id: string;
  displayName: string | null;
  description: string | null;
  membershipType: string | null;
  isArchived: boolean | null;
}
