import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private http: HttpClient) {}


  //[
//   {
//     "phaseDefinitionId": "481224b6-ad2c-4c90-8780-97a1178cf57b",
//     "isEnabled": false,
//     "phaseName": "Spring",
//     "startDate": "2026-01-29T22:01:40.2733376+01:00",
//     "endDate": "2026-04-29T22:01:40.2733389+02:00"
//   },
//   {
//     "phaseDefinitionId": "596b9276-3409-46af-8abc-09b38adf314f",
//     "isEnabled": true,
//     "phaseName": "Winter",
//     "startDate": "2025-10-29T22:01:40.262695+01:00",
//     "endDate": "2026-01-29T22:01:40.2731687+01:00"
//   }
// ]


  getHello(): Observable<{ message: string }> {
    // gleicher Origin -> kein CORS-Problem
    console.log('Making request to /api/hello');
    return this.http.get<{ message: string }>('/api/users');
  }

  getPhases(): Observable<any[]> {
    console.log('Making request to /api/phases');
    return this.http.get<any[]>('/api/users/8dbc59d9-6899-4e8d-9bfb-6bb23a0207dd/phases');
  }
}
