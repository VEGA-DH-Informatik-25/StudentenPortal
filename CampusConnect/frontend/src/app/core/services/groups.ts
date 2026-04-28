import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CampusGroup, CreateGroupRequest, GroupSettingsDetails, UpdateGroupAssignmentsRequest, UpdateGroupSettingsRequest } from '../models/group.model';

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

  updateAssignments(id: string, req: UpdateGroupAssignmentsRequest): Observable<GroupSettingsDetails> {
    return this._http.put<GroupSettingsDetails>(`/api/groups/${id}/assignments`, req);
  }
}
