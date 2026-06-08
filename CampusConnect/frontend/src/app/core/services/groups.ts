import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AddGroupCourseRequest,
  AddGroupMembersRequest,
  CampusGroup,
  CreateGroupRequest,
  GroupCandidate,
  GroupSettingsDetails,
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
}
