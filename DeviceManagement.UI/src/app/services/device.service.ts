import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Device } from '../models/device.model';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DeviceService {
  private http = inject(HttpClient);
  private url = `${environment.apiUrl}/api/devices`;

  getAll(): Observable<Device[]> {
    return this.http.get<Device[]>(this.url);
  }

  search(query: string): Observable<Device[]> {
    return this.http.get<Device[]>(`${this.url}/search`, { params: { q: query } });
  }

  getById(id: string): Observable<Device> {
    return this.http.get<Device>(`${this.url}/${id}`);
  }

  create(device: Device): Observable<Device> {
    return this.http.post<Device>(this.url, device);
  }

  update(id: string, device: Device): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, device);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }

  assign(id: string): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}/assign`, {});
  }

  unassign(id: string): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}/unassign`, {});
  }
}
