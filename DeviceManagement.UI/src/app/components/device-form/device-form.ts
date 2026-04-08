import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DeviceService } from '../../services/device.service';
import { UserService } from '../../services/user.service';
import { Device } from '../../models/device.model';
import { User } from '../../models/user.model';
import { forkJoin, Observable } from 'rxjs';

@Component({
  selector: 'app-device-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './device-form.html',
  styleUrl: './device-form.css'
})
export class DeviceFormComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private deviceService = inject(DeviceService);
  private userService = inject(UserService);

  isEditMode = signal(false);
  deviceId = signal<string | null>(null);
  users = signal<User[]>([]);
  allDevices = signal<Device[]>([]);
  loading = signal(true);
  saving = signal(false);
  error = signal<string | null>(null);

  form = this.fb.group({
    name:         ['', Validators.required],
    manufacturer: ['', Validators.required],
    type:         ['', Validators.required],
    os:           ['', Validators.required],
    osVersion:    ['', Validators.required],
    processor:    ['', Validators.required],
    ramAmount:    [null as number | null, [Validators.required, Validators.min(1)]],
    description:  ['', Validators.required],
    userId:       ['' as string | null]
  });

  readonly deviceTypes = ['phone', 'tablet', 'laptop', 'desktop', 'other'];

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    this.isEditMode.set(!!id);
    this.deviceId.set(id);

    const sources: Record<string, any> = {
      users: this.userService.getAll(),
      devices: this.deviceService.getAll()
    };

    if (id) {
      sources['device'] = this.deviceService.getById(id);
    }

    forkJoin(sources).subscribe({
      next: (results: any) => {
        this.users.set(results['users']);
        this.allDevices.set(results['devices']);

        if (results['device']) {
          const d: Device = results['device'];
          this.form.patchValue({
            name: d.name,
            manufacturer: d.manufacturer,
            type: d.type,
            os: d.os,
            osVersion: d.osVersion,
            processor: d.processor,
            ramAmount: d.ramAmount,
            description: d.description,
            userId: d.userId ?? null
          });
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Nu s-au putut încărca datele.');
        this.loading.set(false);
      }
    });
  }

  save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const name = this.form.value.name!.trim();

    if (!this.isEditMode()) {
      const exists = this.allDevices().some(
        d => d.name.toLowerCase() === name.toLowerCase()
      );
      if (exists) {
        this.error.set(`Un dispozitiv cu numele "${name}" există deja.`);
        return;
      }
    }

    this.saving.set(true);
    this.error.set(null);

    const payload: Device = {
      name:         name,
      manufacturer: this.form.value.manufacturer!.trim(),
      type:         this.form.value.type!,
      os:           this.form.value.os!.trim(),
      osVersion:    this.form.value.osVersion!.trim(),
      processor:    this.form.value.processor!.trim(),
      ramAmount:    this.form.value.ramAmount!,
      description:  this.form.value.description!.trim(),
      userId:       this.form.value.userId || undefined
    };

    const request: Observable<unknown> = this.isEditMode()
      ? this.deviceService.update(this.deviceId()!, payload)
      : this.deviceService.create(payload);

    request.subscribe({
      next: () => this.router.navigate(['/devices']),
      error: () => {
        this.error.set('Salvarea a eșuat. Încearcă din nou.');
        this.saving.set(false);
      }
    });
  }

  isInvalid(field: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.invalid && ctrl?.touched);
  }
}
