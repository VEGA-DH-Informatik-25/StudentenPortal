export type GroupType = 'Course' | 'Official' | 'Campus';
export type GroupRole = 'None' | 'Member' | 'Moderator' | 'Owner';
export type GroupJoinRule = 'Open' | 'RequestRequired' | 'InviteOnly';

export interface GroupSettings {
  allowStudentPosts: boolean;
  allowComments: boolean;
  requiresApproval: boolean;
  isDiscoverable: boolean;
  joinRule: GroupJoinRule;
}

export interface CampusGroup {
  id: string;
  name: string;
  description: string;
  type: GroupType;
  audience: string;
  courseCode: string | null;
  officialCategory: string | null;
  ownerUserId: string | null;
  ownerLabel: string;
  iconLabel: string;
  accentColor: string;
  assignedUserCount: number;
  isAssigned: boolean;
  canManage: boolean;
  canEditSettings: boolean;
  canManageMembers: boolean;
  canAppointModerator: boolean;
  canPost: boolean;
  canInteract: boolean;
  canJoin: boolean;
  canRequestJoin: boolean;
  hasPendingJoinRequest: boolean;
  hasPendingInvitation: boolean;
  pendingJoinRequestCount: number;
  canDelete: boolean;
  groupRole: GroupRole;
  isSystemAdminAccess: boolean;
  isCourseManaged: boolean;
  settings: GroupSettings;
}

export interface CreateGroupRequest {
  name: string;
  description: string;
  type: GroupType;
  audience: string;
  courseCode: string | null;
  officialCategory: string | null;
  allowStudentPosts: boolean;
  allowComments: boolean;
  requiresApproval: boolean;
  isDiscoverable: boolean;
  joinRule: GroupJoinRule;
}

export interface UpdateGroupSettingsRequest extends GroupSettings {}

export interface GroupMember {
  id: string;
  displayName: string;
  email: string;
  role: string;
  course: string;
  groupRole: GroupRole;
  isOwner: boolean;
}

export interface GroupCandidate {
  id: string;
  displayName: string;
  email: string;
  role: string;
  course: string;
}

export interface GroupRequest {
  id: string;
  displayName: string;
  email: string;
  role: string;
  course: string;
}

export interface GroupSettingsDetails {
  group: CampusGroup;
  members: GroupMember[];
  joinRequests: GroupRequest[];
  invitations: GroupRequest[];
}

export interface AddGroupMembersRequest {
  userIds: string[];
}

export interface InviteGroupMembersRequest {
  userIds: string[];
}

export interface AddGroupCourseRequest {
  courseCode: string;
}

export interface SetGroupMemberRoleRequest {
  role: GroupRole;
}
