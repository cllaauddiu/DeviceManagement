import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { GenerateDescriptionRequest, GenerateDescriptionResponse } from '../models/ai-description.model';

@Injectable({ providedIn: 'root' })
export class AiDescriptionService {
  private http = inject(HttpClient);
  private url = `${environment.aiApiUrl}/api/descriptions`;

  generate(request: GenerateDescriptionRequest): Observable<GenerateDescriptionResponse> {
    return this.http.post<GenerateDescriptionResponse>(`${this.url}/generate`, request);
  }
}
