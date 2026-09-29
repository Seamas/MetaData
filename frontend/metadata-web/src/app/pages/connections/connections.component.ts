import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { NzRadioModule } from 'ng-zorro-antd/radio';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzGridModule } from 'ng-zorro-antd/grid';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzMessageService } from 'ng-zorro-antd/message';
import { HttpApiService } from '../../core/api.service';
import {
  ConnectionDto,
  ConnectionTestResultDto,
  DB_TYPE_OPTIONS,
  DatabaseType,
  MetadataImportRequest,
  MetadataImportResultDto,
  SourceTableDto
} from '../../core/models';

@Component({
  selector: 'app-connections',
  standalone: true,
  imports: [
    FormsModule,
    NzTableModule,
    NzButtonModule,
    NzModalModule,
    NzFormModule,
    NzInputModule,
    NzInputNumberModule,
    NzSelectModule,
    NzSwitchModule,
    NzRadioModule,
    NzTagModule,
    NzDrawerModule,
    NzCheckboxModule,
    NzSpinModule,
    NzPopconfirmModule,
    NzGridModule,
    NzIconModule,
    NzAlertModule,
    NzDividerModule,
    NzEmptyModule
  ],
  templateUrl: './connections.component.html',
  styleUrl: './connections.component.scss'
})
export class ConnectionsComponent implements OnInit {
  private api = inject(HttpApiService);
  private message = inject(NzMessageService);

  readonly dbTypeOptions = DB_TYPE_OPTIONS;

  connections: ConnectionDto[] = [];
  loading = false;

  // 编辑模态框
  modalVisible = false;
  modalSaving = false;
  modalTesting = false;
  testResult: ConnectionTestResultDto | null = null;
  editing: ConnectionDto = this.emptyForm();

  // 导入抽屉
  drawerVisible = false;
  drawerConnection: ConnectionDto | null = null;
  sourceLoading = false;
  sourceTables: SourceTableDto[] = [];
  sourceKeyword = '';
  schemaFilter = '';
  sourceError = '';
  checkedTables = new Set<string>();
  importing = false;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.api
      .get<ConnectionDto[]>('/api/connections/list')
      .subscribe({
        next: (list) => (this.connections = list),
        complete: () => (this.loading = false)
      });
  }

  dbLabel(type: DatabaseType): string {
    return this.dbTypeOptions.find((o) => o.value === type)?.label ?? type;
  }

  dbDatabaseLabel(type: DatabaseType): string {
    return this.dbTypeOptions.find((o) => o.value === type)?.databaseLabel ?? '数据库名';
  }

  get isOracle(): boolean {
    return this.editing.databaseType === 'Oracle';
  }

  get isSqlServer(): boolean {
    return this.editing.databaseType === 'SqlServer';
  }

  emptyForm(): ConnectionDto {
    return {
      id: 0,
      name: '',
      databaseType: 'MySql',
      inputMode: 'Simple',
      host: '',
      port: 3306,
      databaseName: '',
      userName: '',
      password: '',
      authMode: 'Basic',
      instanceName: '',
      oracleTargetType: 'ServiceName',
      extraOptions: '',
      advancedConnectionString: '',
      defaultSchema: '',
      isEnabled: true,
      remark: ''
    };
  }

  onTypeChange(type: DatabaseType): void {
    const opt = this.dbTypeOptions.find((o) => o.value === type);
    this.editing.port = opt?.defaultPort ?? null;
    this.editing.oracleTargetType = 'ServiceName';
    this.editing.authMode = 'Basic';
    this.testResult = null;
  }

  openCreate(): void {
    this.editing = this.emptyForm();
    this.testResult = null;
    this.modalVisible = true;
  }

  openEdit(item: ConnectionDto): void {
    // 深拷贝，密码/高级连接串不从出参回填（留空表示不修改）
    this.editing = {
      ...item,
      password: '',
      advancedConnectionString: item.hasAdvancedConnectionString ? '' : item.advancedConnectionString
    };
    this.testResult = null;
    this.modalVisible = true;
  }

  save(): void {
    if (!this.editing.name.trim()) {
      this.message.warning('请填写连接名称');
      return;
    }
    if (this.editing.inputMode === 'Advanced' && !this.editing.advancedConnectionString && !this.editing.hasAdvancedConnectionString) {
      this.message.warning('请填写高级连接串');
      return;
    }
    this.modalSaving = true;
    this.api
      .post<number>('/api/connections/save', this.editing)
      .subscribe({
        next: (id) => {
          this.message.success(this.editing.id ? '已保存修改' : '连接已创建');
          this.editing.id = this.editing.id || id;
          this.modalVisible = false;
          this.load();
        },
        complete: () => (this.modalSaving = false)
      });
  }

  test(): void {
    this.modalTesting = true;
    this.testResult = null;
    this.api
      .post<ConnectionTestResultDto>('/api/connections/test', this.editing)
      .subscribe({
        next: (r) => {
          this.testResult = r;
          if (r.success) {
            this.message.success('连接成功');
          }
        },
        complete: () => (this.modalTesting = false)
      });
  }

  delete(item: ConnectionDto): void {
    this.api.post('/api/connections/delete', { id: item.id }).subscribe(() => {
      this.message.success('已删除');
      this.load();
    });
  }

  // ---------------- 元数据导入抽屉 ----------------

  openImport(item: ConnectionDto): void {
    this.drawerConnection = item;
    this.schemaFilter = item.defaultSchema ?? '';
    this.sourceKeyword = '';
    this.sourceError = '';
    this.checkedTables.clear();
    this.sourceTables = [];
    this.drawerVisible = true;
    this.fetchSourceTables();
  }

  fetchSourceTables(): void {
    if (!this.drawerConnection) return;
    this.sourceLoading = true;
    this.sourceError = '';
    this.sourceTables = [];
    this.checkedTables.clear();
    this.api
      .get<SourceTableDto[]>('/api/metadata/source-tables', {
        connectionId: this.drawerConnection.id,
        schema: this.schemaFilter
      })
      .subscribe({
        next: (list) => (this.sourceTables = list),
        error: (err) => (this.sourceError = HttpApiService.extractMessage(err)),
        complete: () => (this.sourceLoading = false)
      });
  }

  tableKey(t: SourceTableDto): string {
    return `${t.schema ?? ''}|${t.tableName}`;
  }

  get filteredSources(): SourceTableDto[] {
    const kw = this.sourceKeyword.trim().toLowerCase();
    if (!kw) return this.sourceTables;
    return this.sourceTables.filter(
      (t) =>
        t.tableName.toLowerCase().includes(kw) ||
        (t.comment ?? '').toLowerCase().includes(kw)
    );
  }

  allCheckedValue(): boolean {
    const visible = this.filteredSources;
    return visible.length > 0 && visible.every((t) => this.checkedTables.has(this.tableKey(t)));
  }

  indeterminateValue(): boolean {
    const visible = this.filteredSources;
    const checkedCount = visible.filter((t) => this.checkedTables.has(this.tableKey(t))).length;
    return checkedCount > 0 && checkedCount < visible.length;
  }

  onAllChecked(checked: boolean): void {
    for (const t of this.filteredSources) {
      if (checked) this.checkedTables.add(this.tableKey(t));
      else this.checkedTables.delete(this.tableKey(t));
    }
  }

  onItemChecked(t: SourceTableDto, checked: boolean): void {
    if (checked) this.checkedTables.add(this.tableKey(t));
    else this.checkedTables.delete(this.tableKey(t));
  }

  isChecked(t: SourceTableDto): boolean {
    return this.checkedTables.has(this.tableKey(t));
  }

  importSelected(): void {
    if (!this.drawerConnection || this.checkedTables.size === 0) {
      this.message.warning('请先勾选要导入的表');
      return;
    }
    const wanted = this.sourceTables.filter((t) => this.checkedTables.has(this.tableKey(t)));
    const req: MetadataImportRequest = {
      connectionId: this.drawerConnection.id,
      tables: wanted.map((t) => ({ schema: t.schema, tableName: t.tableName }))
    };
    this.importing = true;
    this.api
      .post<MetadataImportResultDto>('/api/metadata/import', req)
      .subscribe({
        next: (r) => {
          const missing =
            r.missingFields.length > 0
              ? `；保留的失效字段 ${r.missingFields.length} 个（未自动删除）`
              : '';
          this.message.success(
            `导入完成：新增表 ${r.addedTables}，更新表 ${r.updatedTables}，新增字段 ${r.addedFields}，更新字段 ${r.updatedFields}${missing}`
          );
          this.drawerVisible = false;
        },
        complete: () => (this.importing = false)
      });
  }
}
