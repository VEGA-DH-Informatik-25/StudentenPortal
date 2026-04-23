import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MensaDay } from '../models/mensa.model';

@Injectable({ providedIn: 'root' })
export class Mensa {
  private readonly _http = inject(HttpClient);

  getWeekMenu(): Observable<MensaDay[]> {
    return this._http.get<MensaDay[]>('/api/mensa');
  }
}

