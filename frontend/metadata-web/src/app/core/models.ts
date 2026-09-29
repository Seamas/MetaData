/** 与后端枚举保持一致（后端以字符串形式序列化枚举）。 */
export type DatabaseType = 'MySql' | 'PostgreSql' | 'SqlServer' | 'Db2' | 'Oracle';
export type ConnectionInputMode = 'Simple' | 'Advanced';
export type AuthMode = 'Basic' | 'Integrated';
export type OracleTargetType = 'ServiceName' | 'Sid';
export type DataCategory =
  | 'Unknown'
  | 'String'
  | 'Number'
  | 'DateTime'
  | 'Boolean'
  | 'Guid'
  | 'Binary';
export type FilterOperator =
  | 'Equal'
  | 'NotEqual'
  | 'Contains'
  | 'StartsWith'
  | 'EndsWith'
  | 'GreaterThan'
  | 'GreaterThanOrEqual'
  | 'LessThan'
  | 'LessThanOrEqual'
  | 'Between'
  | 'IsNull'
  | 'IsNotNull';
export type SortDirection = 'Asc' | 'Desc';

export interface DbTypeOption {
  value: DatabaseType;
  label: string;
  defaultPort: number;
  /** 简单模式下“数据库名”输入框的标签。 */
  databaseLabel: string;
}

export const DB_TYPE_OPTIONS: DbTypeOption[] = [
  { value: 'MySql', label: 'MySQL', defaultPort: 3306, databaseLabel: '数据库名' },
  { value: 'PostgreSql', label: 'PostgreSQL', defaultPort: 5432, databaseLabel: '数据库名' },
  { value: 'SqlServer', label: 'SQL Server', defaultPort: 1433, databaseLabel: '数据库名' },
  { value: 'Db2', label: 'IBM DB2', defaultPort: 50000, databaseLabel: '数据库名' },
  { value: 'Oracle', label: 'Oracle', defaultPort: 1521, databaseLabel: '服务名 / SID' }
];

export interface ConnectionDto {
  id: number;
  name: string;
  databaseType: DatabaseType;
  inputMode: ConnectionInputMode;
  host?: string | null;
  port?: number | null;
  databaseName?: string | null;
  userName?: string | null;
  /** 仅入参；编辑时留空表示不修改。 */
  password?: string | null;
  authMode: AuthMode;
  instanceName?: string | null;
  oracleTargetType?: OracleTargetType | null;
  extraOptions?: string | null;
  advancedConnectionString?: string | null;
  serverVersion?: string | null;
  defaultSchema?: string | null;
  isEnabled: boolean;
  remark?: string | null;
  hasPassword?: boolean;
  hasAdvancedConnectionString?: boolean;
}

export interface ConnectionTestResultDto {
  success: boolean;
  serverVersion?: string | null;
  message?: string | null;
}

export interface TableDto {
  id: number;
  connectionId: number;
  schema?: string | null;
  tableName: string;
  displayName?: string | null;
  isPublished: boolean;
  defaultSortField?: string | null;
  remark?: string | null;
  fieldCount: number;
}

export interface FieldDto {
  id: number;
  tableId: number;
  fieldName: string;
  alias: string;
  displayName?: string | null;
  ordinal: number;
  isVisible: boolean;
  nativeDataType?: string | null;
  dataCategory: DataCategory;
  maxLength?: number | null;
  numericPrecision?: number | null;
  numericScale?: number | null;
  isNullable: boolean;
  isPrimaryKey: boolean;
  remark?: string | null;
}

export interface SourceTableDto {
  schema?: string | null;
  tableName: string;
  comment?: string | null;
}

export interface ImportRequestItem {
  schema?: string | null;
  tableName: string;
}

export interface MetadataImportRequest {
  connectionId: number;
  tables: ImportRequestItem[];
}

export interface MetadataImportResultDto {
  addedTables: number;
  updatedTables: number;
  addedFields: number;
  updatedFields: number;
  missingFields: string[];
}

export interface SaveFieldsRequest {
  tableId: number;
  fields: FieldDto[];
}

export interface PublishedTableDto {
  tableId: number;
  connectionId: number;
  connectionName: string;
  schema?: string | null;
  tableName: string;
  displayName?: string | null;
}

export interface OperatorDescriptor {
  operator: FilterOperator;
  label: string;
  needsValue: boolean;
  needsValue2: boolean;
}

export interface CategoryOperators {
  dataCategory: DataCategory;
  label: string;
  operators: OperatorDescriptor[];
}

export interface FilterDto {
  alias: string;
  operator: FilterOperator;
  value?: unknown;
  value2?: unknown;
}

export interface SortDto {
  alias: string;
  direction: SortDirection;
}

export interface DataQueryRequest {
  tableId: number;
  page: number;
  pageSize: number;
  filters: FilterDto[];
  sorts: SortDto[];
}

export interface ColumnDto {
  alias: string;
  displayName?: string | null;
  dataCategory: DataCategory;
  nativeDataType?: string | null;
  width?: number | null;
  isPrimaryKey: boolean;
}

export interface DataQueryResponse {
  columns: ColumnDto[];
  rows: Record<string, unknown>[];
  total: number;
  page: number;
  pageSize: number;
}

export interface FieldPreferenceDto {
  fieldId: number;
  ordinal: number;
  isVisible: boolean;
  width?: number | null;
}

export const DATA_CATEGORY_OPTIONS: { value: DataCategory; label: string }[] = [
  { value: 'Unknown', label: '其他' },
  { value: 'String', label: '字符串' },
  { value: 'Number', label: '数字' },
  { value: 'DateTime', label: '日期时间' },
  { value: 'Boolean', label: '布尔' },
  { value: 'Guid', label: '唯一标识' },
  { value: 'Binary', label: '二进制' }
];
