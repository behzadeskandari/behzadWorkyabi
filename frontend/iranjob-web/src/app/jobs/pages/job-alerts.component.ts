import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { City, Country, JobCategory, Province, Skill, WorkArrangement } from '../../reference-data/models/reference-data.models';
import { JobCategoryService } from '../../reference-data/services/job-category.service';
import { LocationService } from '../../reference-data/services/location.service';
import { SkillService } from '../../reference-data/services/skill.service';
import { WorkArrangementService } from '../../reference-data/services/work-arrangement.service';
import { CandidateJobPreferencesService } from '../services/candidate-job-preferences.service';
import { JobAlert, JobAlertRequest } from '../models/candidate-job-preferences.models';

@Component({ selector: 'app-job-alerts', standalone: true, imports: [ReactiveFormsModule], templateUrl: './job-alerts.component.html', styleUrl: './job-alerts.component.scss' })
export class JobAlertsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly prefs = inject(CandidateJobPreferencesService);
  private readonly locations = inject(LocationService);
  private readonly categoriesApi = inject(JobCategoryService);
  private readonly skillsApi = inject(SkillService);
  private readonly arrangementsApi = inject(WorkArrangementService);
  readonly form = this.fb.nonNullable.group({ name: 'هشدار شغلی من', categoryId: '', countryId: '', provinceId: '', cityId: '', workArrangement: '' });
  alerts: JobAlert[] = [];
  categories: JobCategory[] = [];
  countries: Country[] = [];
  provinces: Province[] = [];
  cities: City[] = [];
  skills: Skill[] = [];
  arrangements: WorkArrangement[] = [];
  selectedSkills = new Set<string>();
  editingId: string | null = null;
  loading = true;
  saving = false;
  error = false;

  ngOnInit(): void {
    this.loadAlerts();
    this.categoriesApi.getAll().subscribe({ next: x => this.categories = x, error: () => this.error = true });
    this.skillsApi.getAll().subscribe({ next: x => this.skills = x, error: () => this.error = true });
    this.arrangementsApi.getAll().subscribe({ next: x => this.arrangements = x, error: () => this.error = true });
    this.locations.getCountries().subscribe({ next: x => this.countries = x, error: () => this.error = true });
  }

  loadAlerts(): void { this.loading = true; this.prefs.listAlerts().subscribe({ next: x => { this.alerts = x; this.loading = false; }, error: () => { this.error = true; this.loading = false; } }); }
  countryChanged(): void { this.provinces = []; this.cities = []; this.form.controls.provinceId.setValue(''); this.form.controls.cityId.setValue(''); const id = this.form.controls.countryId.value; if (id) this.locations.getProvinces(id).subscribe({ next: x => this.provinces = x, error: () => this.error = true }); }
  provinceChanged(): void { this.cities = []; this.form.controls.cityId.setValue(''); const id = this.form.controls.provinceId.value; if (id) this.locations.getCities(id).subscribe({ next: x => this.cities = x, error: () => this.error = true }); }
  toggleSkill(id: string, checked: boolean): void { if (checked) this.selectedSkills.add(id); else this.selectedSkills.delete(id); this.selectedSkills = new Set(this.selectedSkills); }

  edit(alert: JobAlert): void {
    this.editingId = alert.id;
    this.selectedSkills = new Set(alert.skillIds);
    this.form.patchValue({ name: alert.name, categoryId: alert.categoryId ?? '', workArrangement: alert.workArrangement ?? '', cityId: alert.cityId ?? '', countryId: '', provinceId: '' });
    if (!alert.cityId) { this.provinces = []; this.cities = []; return; }
    this.locations.getById(alert.cityId).subscribe({ next: city => {
      const provinceId = city.parentId ?? '';
      this.form.patchValue({ countryId: city.countryId ?? '', provinceId });
      if (city.countryId) this.locations.getProvinces(city.countryId).subscribe({ next: x => this.provinces = x, error: () => this.error = true });
      if (provinceId) this.locations.getCities(provinceId).subscribe({ next: x => this.cities = x, error: () => this.error = true });
    }, error: () => this.error = true });
  }

  cancelEdit(): void { this.editingId = null; this.selectedSkills.clear(); this.form.reset({ name: 'هشدار شغلی من', categoryId: '', countryId: '', provinceId: '', cityId: '', workArrangement: '' }); this.provinces = []; this.cities = []; }
  submit(): void {
    const value = this.form.getRawValue();
    if (!value.name.trim() || this.selectedSkills.size > 30) { this.error = true; return; }
    const request: JobAlertRequest = { name: value.name.trim(), categoryId: value.categoryId || null, cityId: value.cityId || null, workArrangement: value.workArrangement || null, skillIds: [...this.selectedSkills], isEnabled: this.editingId ? this.alerts.find(x => x.id === this.editingId)?.isEnabled ?? true : true };
    this.saving = true; this.error = false;
    const call = this.editingId ? this.prefs.updateAlert(this.editingId, request) : this.prefs.createAlert(request);
    call.subscribe({ next: () => { this.saving = false; this.cancelEdit(); this.loadAlerts(); }, error: () => { this.saving = false; this.error = true; } });
  }
  toggle(alert: JobAlert): void { const request = this.toRequest(alert, !alert.isEnabled); this.prefs.updateAlert(alert.id, request).subscribe({ next: updated => this.alerts = this.alerts.map(x => x.id === updated.id ? updated : x), error: () => this.error = true }); }
  remove(alert: JobAlert): void { this.prefs.deleteAlert(alert.id).subscribe({ next: () => this.alerts = this.alerts.filter(x => x.id !== alert.id), error: () => this.error = true }); }
  private toRequest(a: JobAlert, enabled: boolean): JobAlertRequest { return { name: a.name, categoryId: a.categoryId, cityId: a.cityId, workArrangement: a.workArrangement, skillIds: a.skillIds, isEnabled: enabled }; }
}
