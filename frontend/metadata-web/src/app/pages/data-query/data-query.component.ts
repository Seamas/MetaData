import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTableModule, NzTableSortOrder } from 'ng-zorro-antd/table';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzGridModule } from 'ng-zorro-antd/grid';
import { NzMessageService } from 'ng-zorro-antd/message';
import { HttpApiService } from '../../core/api.service';
import {
  CategoryOperators,
  DataQueryRequest,
  DataQueryResponse,
  FieldDto,
  FieldPreferenceDto,
  FilterDto,
  FilterOperator,
  PublishedTableDto
} from '../../core/models';

interface FilterRow {
  field: FieldDto | null;
  operator: FilterOperator | null;
  value: unknown;
  value2: unknown;
}

@Component({
  selector: 'app-data-query',
  standalone: true,
  imports: [
    FormsModule,
    DragDropModule,
    NzSelectModule,
    NzTableModule,
    NzButtonModule,
    NzInputModule,
    NzInputNumberModule,
    NzDatePickerModule,
    NzSwitchModule,
    NzTagModule,
    NzSpinModule,
    NzEmptyModule,
    NzDrawerModule,
    NzIconModule,
    NzGridModule
  ],
  templateUrl: './data-query.component.html',
  styleUrl: './data-query.component.scss'
})
export class DataQueryComponent implements OnInit {
  private api = inject(HttpApiService);
  private message = inject(NzMessageService);

  publishedTables: PublishedTableDto[] = [];
  selectedTableId: number | null = null;
  selectedTable: PublishedTableDto | null = null;

  fields: FieldDto[] = [];
  private preferences: FieldPreferenceDto[] = [];
  operatorMap: Record<string, CategoryOperators> = {};

  filters: FilterRow[] = [];
  rows: Record<string, unknown>[] = [];
  total = 0;
  page = 1;
  pageSize = 20;
  loading = false;

  sortKey: string | null = null;
  sortDir: NzTableSortOrder = null;

  drawerVisible = false;
  /** 抽屉中可排序的字段（含被隐藏的）。 */
  drawerFields: { field: FieldDto; ordinal: number; isVisible: boolean; width: number | null }[] = [];
  savingPrefs = false;

  ngOnInit(): void {
    this.api
      .get<CategoryOperators[]>('/api/meta/operators')
      .subscribe((list) => {
        for (const c of list) {
          this.operatorMap[c.dataCategory] = c;
        }
      });
    this.api.get<PublishedTableDto[]>('/api/data/published-tables').subscribe((list) => {
      this.publishedTables = list;
    });
  }

  onTableChange(): void {
    this.selectedTable = this.publishedTables.find((t) => t.tableId === this.selectedTableId) ?? null;
    this.filters = [];
    this.rows = [];
    this.total = 0;
    this.page = 1;
    this.sortKey = null;
    this.sortDir = null;
    if (!this.selectedTableId) return;

    this.loading = true;
    this.api
      .post<FieldDto[]>('/api/metadata/fields', { tableId: this.selectedTableId })
      .subscribe({
        next: (fields) => {
          this.fields = fields;
          this.api
            .post<FieldPreferenceDto[]>('/api/data/preferences', { tableId: this.selectedTableId })
            .subscribe({
              next: (prefs) => {
                this.preferences = prefs;
                this.query();
              },
              complete: () => (this.loading = false)
            });
        },
        error: () => (this.loading = false)
      });
  }

  // ---------- 列展示（合并用户偏好） ----------

  private prefOf(fieldId: number): FieldPreferenceDto | undefined {
    return this.preferences.find((p) => p.fieldId === fieldId);
  }

  get orderedFields(): FieldDto[] {
    return [...this.fields].sort((a, b) => {
      const pa = this.prefOf(a.id)?.ordinal;
      const pb = this.prefOf(b.id)?.ordinal;
      if (pa != null && pb != null) return pa - pb;
      if (pa != null) return -1;
      if (pb != null) return 1;
      return a.ordinal - b.ordinal;
    });
  }

  get visibleFields(): FieldDto[] {
    return this.orderedFields.filter((f) => {
      const pref = this.prefOf(f.id);
      return pref ? pref.isVisible : f.isVisible;
    });
  }

  widthOf(f: FieldDto): number | null {
    return this.prefOf(f.id)?.width ?? null;
  }

  colTitle(f: FieldDto): string {
    return f.displayName || f.alias;
  }

  cellValue(row: Record<string, unknown>, f: FieldDto): string {
    const v = row[f.alias];
    if (v === null || v === undefined) return '';
    if (typeof v === 'boolean') return v ? '是' : '否';
    return String(v);
  }

  // ---------- 筛选 ----------

  operatorsOf(field: FieldDto | null) {
    if (!field) return [];
    return this.operatorMap[field.dataCategory]?.operators ?? this.operatorMap['Unknown']?.operators ?? [];
  }

  opDesc(row: FilterRow) {
    if (!row.field || !row.operator) return undefined;
    return this.operatorsOf(row.field).find((o) => o.operator === row.operator);
  }

  isBetween(op: FilterOperator | null): boolean {
    return op === 'Between';
  }

  isNullOnly(op: FilterOperator | null): boolean {
    return op === 'IsNull' || op === 'IsNotNull';
  }

  addFilter(): void {
    this.filters.push({ field: null, operator: null, value: null, value2: null });
  }

  removeFilter(row: FilterRow): void {
    this.filters = this.filters.filter((r) => r !== row);
  }

  onFilterFieldChange(row: FilterRow): void {
    row.operator = null;
    row.value = null;
    row.value2 = null;
  }

  private formatDate(v: unknown): string | null {
    if (!v) return null;
    const d = v as Date;
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  /** 区间选择器值（仅日期分类使用）。 */
  // nz-range-picker 直接通过 ngModel 写入 row.value（[Date, Date]）

  resetFilters(): void {
    this.filters = [];
    this.page = 1;
    this.query();
  }

  // ---------- 查询 ----------

  query(): void {
    if (!this.selectedTableId) return;
    const filters: FilterDto[] = [];
    for (const row of this.filters) {
      if (!row.field || !row.operator) continue;
      const f = row.field;
      if (this.isNullOnly(row.operator)) {
        filters.push({ alias: f.alias, operator: row.operator });
        continue;
      }
      if (f.dataCategory === 'DateTime' && this.isBetween(row.operator)) {
        const range = row.value as [Date, Date] | null;
        if (!range || range.length !== 2 || !range[0] || !range[1]) continue;
        filters.push({
          alias: f.alias,
          operator: row.operator,
          value: this.formatDate(range[0]),
          value2: this.formatDate(range[1])
        });
        continue;
      }
      if (row.value === null || row.value === undefined || row.value === '') continue;
      let value: unknown = row.value;
      if (f.dataCategory === 'DateTime') value = this.formatDate(row.value);
      if (f.dataCategory === 'Number') value = Number(row.value);
      const item: FilterDto = { alias: f.alias, operator: row.operator, value };
      if (this.isBetween(row.operator)) {
        if (row.value2 === null || row.value2 === undefined || row.value2 === '') continue;
        item.value2 = f.dataCategory === 'Number' ? Number(row.value2) : row.value2;
      }
      filters.push(item);
    }

    const req: DataQueryRequest = {
      tableId: this.selectedTableId,
      page: this.page,
      pageSize: this.pageSize,
      filters,
      sorts: this.sortKey && this.sortDir
        ? [{ alias: this.sortKey, direction: this.sortDir === 'ascend' ? 'Asc' : 'Desc' }]
        : []
    };

    this.loading = true;
    this.api
      .post<DataQueryResponse>('/api/data/query', req)
      .subscribe({
        next: (res) => {
          this.rows = res.rows;
          this.total = res.total;
        },
        complete: () => (this.loading = false)
      });
  }

  onPageChange(page: number): void {
    this.page = page;
    this.query();
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.page = 1;
    this.query();
  }

  onSortChange(sort: { key: string; value: NzTableSortOrder }[]): void {
    const entry = sort.find((s) => s.value != null);
    this.sortKey = entry ? entry.key : null;
    this.sortDir = entry ? entry.value : null;
    this.page = 1;
    this.query();
  }

  // ---------- 个性化列设置 ----------

  openDrawer(): void {
    this.drawerFields = this.orderedFields.map((f) => {
      const pref = this.prefOf(f.id);
      return {
        field: f,
        ordinal: pref?.ordinal ?? f.ordinal,
        isVisible: pref ? pref.isVisible : f.isVisible,
        width: pref?.width ?? null
      };
    });
    this.drawerVisible = true;
  }

  dropPref(event: CdkDragDrop<unknown[]>): void {
    moveItemInArray(this.drawerFields, event.previousIndex, event.currentIndex);
  }

  savePreferences(): void {
    if (!this.selectedTableId) return;
    const items: FieldPreferenceDto[] = this.drawerFields.map((d, i) => ({
      fieldId: d.field.id,
      ordinal: i + 1,
      isVisible: d.isVisible,
      width: d.width
    }));
    this.savingPrefs = true;
    this.api
      .post('/api/data/preferences/save', { tableId: this.selectedTableId, items })
      .subscribe({
        next: () => {
          this.message.success('列设置已保存（仅对当前用户生效）');
          this.preferences = items;
          this.drawerVisible = false;
        },
        complete: () => (this.savingPrefs = false)
      });
  }
}
