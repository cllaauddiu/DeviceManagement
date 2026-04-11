import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DeviceService } from '../../services/device.service';
import { UserService } from '../../services/user.service';
import { AuthService } from '../../services/auth.service';
import { AiDescriptionService } from '../../services/ai-description.service';
import { Device } from '../../models/device.model';
import { User } from '../../models/user.model';
import { catchError, debounceTime, distinctUntilChanged, forkJoin, of, Subject, switchMap, tap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-device-list',
  imports: [RouterLink],
  templateUrl: './device-list.html',
  styleUrl: './device-list.css'
})
export class DeviceListComponent implements OnInit {
  private deviceService = inject(DeviceService);
  private userService = inject(UserService);
  private authService = inject(AuthService);
  private aiDescriptionService = inject(AiDescriptionService);
  private destroyRef = inject(DestroyRef);
  private searchInput$ = new Subject<string>();

  allDevices = signal<Device[]>([]);
  devices = signal<Device[]>([]);
  users = signal<User[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  searchQuery = signal('');
  searching = signal(false);
  searchError = signal<string | null>(null);

  modalOpen = signal(false);
  selectedDevice = signal<Device | null>(null);
  aiLoading = signal(false);
  aiDescription = signal<string | null>(null);
  aiError = signal<string | null>(null);

  get currentUserId() { return this.authService.currentUser()?.id; }

  ngOnInit() {
    this.initializeSearch();

    forkJoin({
      devices: this.deviceService.getAll(),
      users: this.userService.getAll()
    }).subscribe({
      next: ({ devices, users }) => {
        this.allDevices.set(devices);
        this.devices.set(devices);
        this.users.set(users);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Nu s-au putut încărca datele. Verifică că API-ul rulează.');
        this.loading.set(false);
      }
    });
  }

  getUserName(userId?: string): string {
    if (!userId) return '—';
    const user = this.users().find(u => u.id === userId);
    return user ? user.name : '—';
  }

  assign(device: Device) {
    this.deviceService.assign(device.id!).subscribe({
      next: () => this.updateDeviceInLists(device.id!, d => ({ ...d, userId: this.currentUserId })),
      error: err => this.error.set(err.status === 409 ? 'Dispozitivul este deja asignat.' : 'Asignarea a eșuat.')
    });
  }

  unassign(device: Device) {
    this.deviceService.unassign(device.id!).subscribe({
      next: () => this.updateDeviceInLists(device.id!, d => ({ ...d, userId: undefined })),
      error: () => this.error.set('Dezasignarea a eșuat.')
    });
  }

  deleteDevice(id: string) {
    if (!confirm('Ești sigur că vrei să ștergi acest dispozitiv?')) return;
    this.deviceService.delete(id).subscribe({
      next: () => {
        this.allDevices.update(list => list.filter(d => d.id !== id));
        this.devices.update(list => list.filter(d => d.id !== id));
      },
      error: () => this.error.set('Ștergerea a eșuat.')
    });
  }

  onSearchInput(query: string) {
    this.searchQuery.set(query);
    this.searchInput$.next(query);
  }

  openDescriptionModal(device: Device) {
    this.selectedDevice.set(device);
    this.modalOpen.set(true);
    this.aiLoading.set(true);
    this.aiError.set(null);
    this.aiDescription.set(null);

    this.aiDescriptionService.generate({
      brand: device.manufacturer,
      model: device.name,
      type: device.type,
      cpu: device.processor || undefined,
      ramGb: device.ramAmount,
      operatingSystem: `${device.os} ${device.osVersion}`.trim(),
      notes: device.description || undefined
    }).subscribe({
      next: (res) => {
        this.aiDescription.set(res.description);
        this.aiLoading.set(false);
      },
      error: () => {
        this.aiError.set('Nu s-a putut genera descrierea AI. Verifică dacă serviciul AI rulează.');
        this.aiLoading.set(false);
      }
    });
  }

  closeDescriptionModal() {
    this.modalOpen.set(false);
    this.selectedDevice.set(null);
    this.aiLoading.set(false);
    this.aiDescription.set(null);
    this.aiError.set(null);
  }

  private initializeSearch() {
    this.searchInput$.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap((rawQuery) => {
        const query = rawQuery.trim();
        this.searchError.set(null);

        if (!query) {
          this.searching.set(false);
          this.devices.set(this.allDevices());
          return of<Device[] | null>(null);
        }

        this.searching.set(true);
        return this.deviceService.search(query).pipe(
          tap(() => this.searching.set(false)),
          catchError(() => {
            this.searching.set(false);
            this.searchError.set('Căutarea a eșuat. Reîncearcă.');
            return of(this.devices());
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((results) => {
      if (results !== null) {
        this.devices.set(results);
      }
    });
  }

  private updateDeviceInLists(id: string, updater: (device: Device) => Device) {
    this.allDevices.update(list => list.map(d => d.id === id ? updater(d) : d));
    this.devices.update(list => list.map(d => d.id === id ? updater(d) : d));
  }
}
