import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DeviceService } from '../../services/device.service';
import { UserService } from '../../services/user.service';
import { AuthService } from '../../services/auth.service';
import { Device } from '../../models/device.model';
import { User } from '../../models/user.model';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-device-detail',
  imports: [RouterLink],
  templateUrl: './device-detail.html',
  styleUrl: './device-detail.css'
})
export class DeviceDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private deviceService = inject(DeviceService);
  private userService = inject(UserService);
  private authService = inject(AuthService);

  device = signal<Device | null>(null);
  assignedUser = signal<User | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  get currentUserId() { return this.authService.currentUser()?.id; }

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id')!;
    forkJoin({
      device: this.deviceService.getById(id),
      users: this.userService.getAll()
    }).subscribe({
      next: ({ device, users }) => {
        this.device.set(device);
        if (device.userId)
          this.assignedUser.set(users.find(u => u.id === device.userId) ?? null);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Dispozitivul nu a fost găsit.');
        this.loading.set(false);
      }
    });
  }

  assign() {
    const d = this.device()!;
    this.deviceService.assign(d.id!).subscribe({
      next: () => {
        this.device.update(dev => ({ ...dev!, userId: this.currentUserId }));
        this.assignedUser.set(null);
      },
      error: err => this.error.set(err.status === 409 ? 'Dispozitivul este deja asignat.' : 'Asignarea a eșuat.')
    });
  }

  unassign() {
    const d = this.device()!;
    this.deviceService.unassign(d.id!).subscribe({
      next: () => {
        this.device.update(dev => ({ ...dev!, userId: undefined }));
        this.assignedUser.set(null);
      },
      error: () => this.error.set('Dezasignarea a eșuat.')
    });
  }
}
