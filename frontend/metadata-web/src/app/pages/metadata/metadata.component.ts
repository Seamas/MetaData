import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzListModule } from 'ng-zorro-antd/list';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzMessageService } from 'ng-zorro-antd/message';
import { HttpApiService } from '../../core/api.service';
import {
  ConnectionDto,
  DATA_CATEGORY_OPTIONS,
  FieldDto,
  TableDto
} from '../../core/models';

@Component({
  selector: 'app-metadata',
  standalone: true,
  imports: [
    FormsModule,
    DragDropModule,
    NzSelectModule,
    NzListModule,
    NzTableModule,
    NzButtonModule,
    NzInputModule,
    NzSwitchModule,
    NzTagModule,
    NzSpinModule,
    NzPopconfirmModule,
    NzEmptyModule,
    NzDividerModule,
    NzIconModule
  ],
  templateUrl: './metadata.component.html',
  styleUrl: './metadata.component.scss'
})
export class MetadataComponent implements OnInit {
  private api = inject(HttpApiService);
  private message = inject(NzMessageService);

  readonly categoryOptions = DATA_CATEGORY_OPTIONS;

  connections: ConnectionDto[] = [];
  selectedConnectionId: number | null = null;
  tables: TableDto[] = [];
  tablesLoading = false;

  selectedTable: TableDto | null = null;
  fields: FieldDto[] = [];
  fieldsLoading = false;
  savingTable = false;
  savingFields = false;

  // 表属性编辑副本
  tableForm = {
    displayName: '',
    defaultSortField: null as string | null,
    isPublished: false,
    remark: ''
  };

  ngOnInit(): void {
    this.api.get<ConnectionDto[]>('/api/connections/list').subscribe((list) => {
      this.connections = list;
      if (list.length > 0) {
        this.selectedConnectionId = list[0].id;
        this.loadTables();
      }
    });
  }

  onConnectionChange(): void {
    this.selectedTable = null;
    this.fields = [];
    this.loadTables();
  }

  loadTables(): void {
    if (!this.selectedConnectionId) return;
    this.tablesLoading = true;
    this.api
      .post<TableDto[]>('/api/metadata/tables', { connectionId: this.selectedConnectionId })
      .subscribe({
        next: (list) => {
          this.tables = list;
          if (this.selectedTable) {
            const refreshed = list.find((t) => t.id === this.selectedTable!.id);
            if (refreshed) this.selectedTable = refreshed;
          }
        },
        complete: () => (this.tablesLoading = false)
      });
  }

  selectTable(table: TableDto): void {
    this.selectedTable = table;
    this.tableForm = {
      displayName: table.displayName ?? '',
      defaultSortField: table.defaultSortField ?? null,
      isPublished: table.isPublished,
      remark: table.remark ?? ''
    };
    this.fieldsLoading = true;
    this.fields = [];
    this.api
      .post<FieldDto[]>('/api/metadata/fields', { tableId: table.id })
      .subscribe({
        next: (list) => (this.fields = [...list].sort((a, b) => a.ordinal - b.ordinal)),
        complete: () => (this.fieldsLoading = false)
      });
  }

  drop(event: CdkDragDrop<FieldDto[]>): void {
    moveItemInArray(this.fields, event.previousIndex, event.currentIndex);
    this.fields.forEach((f, i) => (f.ordinal = i + 1));
  }

  aliasConflict(field: FieldDto): boolean {
    const v = field.alias.trim();
    if (!v) return true;
    return this.fields.some((f) => f !== field && f.alias.trim() === v);
  }

  get hasAliasIssue(): boolean {
    return this.fields.some((f) => this.aliasConflict(f));
  }

  saveTable(): void {
    if (!this.selectedTable) return;
    this.savingTable = true;
    const dto: TableDto = {
      ...this.selectedTable,
      displayName: this.tableForm.displayName,
      defaultSortField: this.tableForm.defaultSortField,
      isPublished: this.tableForm.isPublished,
      remark: this.tableForm.remark
    };
    this.api.post<number>('/api/metadata/save-table', dto).subscribe({
      next: () => {
        this.message.success('表属性已保存');
        this.loadTables();
      },
      complete: () => (this.savingTable = false)
    });
  }

  saveFields(): void {
    if (!this.selectedTable) return;
    if (this.hasAliasIssue) {
      this.message.error('存在别名重复或为空，请先修正');
      return;
    }
    this.savingFields = true;
    // 序号按当前顺序提交
    const ordered = this.fields.map((f, i) => ({ ...f, ordinal: i + 1 }));
    this.api
      .post('/api/metadata/save-fields', {
        tableId: this.selectedTable.id,
        fields: ordered
      })
      .subscribe({
        next: () => {
          this.message.success('字段配置已保存');
          this.fields = ordered;
        },
        complete: () => (this.savingFields = false)
      });
  }

  deleteTable(): void {
    if (!this.selectedTable) return;
    const id = this.selectedTable.id;
    this.api.post('/api/metadata/delete-table', { id }).subscribe(() => {
      this.message.success('表已删除');
      this.selectedTable = null;
      this.fields = [];
      this.loadTables();
    });
  }

  tableTitle(t: TableDto): string {
    return t.displayName ? `${t.displayName}（${t.tableName}）` : t.tableName;
  }
}
