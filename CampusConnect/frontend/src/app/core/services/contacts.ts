import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ContactProfile } from '../models/contact.model';

@Injectable({ providedIn: 'root' })
export class Contacts {
  private readonly _http = inject(HttpClient);

  searchContacts(query: string): Observable<ContactProfile[]> {
    let params = new HttpParams();
    const term = query.trim();
    if (term) {
      params = params.set('query', term);
    }

    return this._http.get<ContactProfile[]>('/api/contacts', { params });
  }
}
