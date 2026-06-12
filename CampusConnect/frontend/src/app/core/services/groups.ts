import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AddGroupCourseRequest,
  AddGroupMembersRequest,
  CampusGroup,
  CreateGroupRequest,
  GroupCandidate,
  LeaveGroupRequest,
  LeaveGroupResponse,
  GroupSettingsDetails,
  InviteGroupMembersRequest,
  SetGroupMemberRoleRequest,
  UpdateGroupSettingsRequest,
} from '../models/group.model';

@Injectable({ providedIn: 'root' })
export class Groups {
  private readonly _http = inject(HttpClient);

  getGroups(): Observable<CampusGroup[]> {
    return this._http.get<CampusGroup[]>('/api/groups');
  }

  createGroup(req: CreateGroupRequest): Observable<CampusGroup> {
    return this._http.post<CampusGroup>('/api/groups', req);
  }

  getSettings(id: string): Observable<GroupSettingsDetails> {
    return this._http.get<GroupSettingsDetails>(`/api/groups/${id}/settings`);
  }

  updateSettings(id: string, req: UpdateGroupSettingsRequest): Observable<CampusGroup> {
    return this._http.put<CampusGroup>(`/api/groups/${id}/settings`, req);
  }

  deleteGroup(id: string): Observable<void> {
    return this._http.delete<void>(`/api/groups/${id}`);
  }

  searchCandidates(id: string, query: string): Observable<GroupCandidate[]> {
    const params = query ? `?query=${encodeURIComponent(query)}` : '';
    return this._http.get<GroupCandidate[]>(`/api/groups/${id}/candidates${params}`);
  }

  addMembers(id: string, req: AddGroupMembersRequest): Observable<GroupSettingsDetails> {
    return this._http.post<GroupSettingsDetails>(`/api/groups/${id}/members`, req);
  }

  addCourse(id: string, req: AddGroupCourseRequest): Observable<GroupSettingsDetails> {
    return this._http.post<GroupSettingsDetails>(`/api/groups/${id}/members/course`, req);
  }

  removeMember(id: string, userId: string): Observable<GroupSettingsDetails> {
    return this._http.delete<GroupSettingsDetails>(`/api/groups/${id}/members/${userId}`);
  }

  setMemberRole(id: string, userId: string, req: SetGroupMemberRoleRequest): Observable<GroupSettingsDetails> {
    return this._http.put<GroupSettingsDetails>(`/api/groups/${id}/members/${userId}/role`, req);
  }

  joinGroup(id: string): Observable<CampusGroup> {
    return this._http.post<CampusGroup>(`/api/groups/${id}/join`, {});
  }

  leaveGroup(id: string, req: LeaveGroupRequest = { newOwnerUserId: null }): Observable<LeaveGroupResponse> {
    return this._http.post<LeaveGroupResponse>(`/api/groups/${id}/leave`, req);
  }

  approveRequest(id: string, userId: string): Observable<GroupSettingsDetails> {
    return this._http.post<GroupSettingsDetails>(`/api/groups/${id}/requests/${userId}/approve`, {});
  }

  rejectRequest(id: string, userId: string): Observable<GroupSettingsDetails> {
    return this._http.post<GroupSettingsDetails>(`/api/groups/${id}/requests/${userId}/reject`, {});
  }

  inviteMembers(id: string, req: InviteGroupMembersRequest): Observable<GroupSettingsDetails> {
    return this._http.post<GroupSettingsDetails>(`/api/groups/${id}/invitations`, req);
  }

  cancelInvitation(id: string, userId: string): Observable<GroupSettingsDetails> {
    return this._http.delete<GroupSettingsDetails>(`/api/groups/${id}/invitations/${userId}`);
  }

  acceptInvitation(id: string): Observable<CampusGroup> {
    return this._http.post<CampusGroup>(`/api/groups/${id}/invitations/accept`, {});
  }

  declineInvitation(id: string): Observable<CampusGroup> {
    return this._http.post<CampusGroup>(`/api/groups/${id}/invitations/decline`, {});
  }
}
