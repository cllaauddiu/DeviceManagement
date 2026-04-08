import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { DeviceListComponent } from './components/device-list/device-list';
import { DeviceDetailComponent } from './components/device-detail/device-detail';
import { DeviceFormComponent } from './components/device-form/device-form';
import { LoginComponent } from './components/login/login';
import { RegisterComponent } from './components/register/register';

export const routes: Routes = [
  { path: '', redirectTo: 'devices', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'devices', component: DeviceListComponent, canActivate: [authGuard] },
  { path: 'devices/new', component: DeviceFormComponent, canActivate: [authGuard] },
  { path: 'devices/:id', component: DeviceDetailComponent, canActivate: [authGuard] },
  { path: 'devices/:id/edit', component: DeviceFormComponent, canActivate: [authGuard] },
];
