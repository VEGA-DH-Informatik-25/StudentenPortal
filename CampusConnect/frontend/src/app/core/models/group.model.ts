export type GroupType = 'Course' | 'Official' | 'Social';
export type GroupMemberPermission = 'ReadOnly' | 'ReadWrite' | 'Manage';

export interface GroupSettings {
  allowStudentPosts: boolean;
  allowComments: boolean;
  requiresApproval: boolean;
  isDiscoverable: boolean;
}

export interface CampusGroup {
  id: string;
  name: string;
  description: string;
  type: GroupType;
  audience: string;
  courseCode: string | null;
  ownerUserId: string | null;
  ownerLabel: string;
  iconLabel: string;
  accentColor: string;
  assignedUserCount: number;
  canManage: boolean;
  isAssigned: boolean;
  canPost: boolean;
  canJoin: boolean;
  memberPermission: GroupMemberPermission;
  settings: GroupSettings;
}

export interface CreateGroupRequest {
  name: string;
  description: string;
  audience: string;
  allowStudentPosts: boolean;
  allowComments: boolean;
  requiresApproval: boolean;
  isDiscoverable: boolean;
}

export interface UpdateGroupSettingsRequest extends GroupSettings {}

export interface GroupAccount {
  id: string;
  displayName: string;
  email: string;
  role: string;
  course: string;
  isAssigned: boolean;
  permission: GroupMemberPermission;
}

export interface GroupSettingsDetails {
  group: CampusGroup;
  accounts: GroupAccount[];
}

export interface UpdateGroupAssignmentsRequest {
  userIds: string[];
}

export interface UpdateGroupMemberPermissionItem {
  userId: string;
  permission: GroupMemberPermission;
}

export interface UpdateGroupMemberPermissionsRequest {
  permissions: UpdateGroupMemberPermissionItem[];
}
