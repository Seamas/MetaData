import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { NzMessageService } from 'ng-zorro-antd/message';

/**
 * 统一 HTTP 封装：本系统仅使用 GET / POST。
 * 后端业务异常返回 400 + { success:false, message }，这里统一弹出错误提示。
 */
@Injectable({ providedIn: 'root' })
export class HttpApiService {
  private http = inject(HttpClient);
  private message = inject(NzMessageService);

  get<T>(
    url: string,
    params?: Record<string, string | number | boolean | undefined | null>
  ): Observable<T> {
    let httpParams = new HttpParams();
    if (params) {
      for (const [k, v] of Object.entries(params)) {
        if (v !== undefined && v !== null && v !== '') {
          httpParams = httpParams.set(k, String(v));
        }
      }
    }
    return this.http.get<T>(url, { params: httpParams }).pipe(
      catchError((err) => this.handleError(err))
    );
  }

  post<T>(url: string, body: unknown): Observable<T> {
    return this.http
      .post<T>(url, body ?? {})
      .pipe(catchError((err) => this.handleError(err)));
  }

  /** 提取后端业务异常消息。 */
  static extractMessage(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      if (err.error && typeof err.error === 'object' && 'message' in err.error) {
        return String((err.error as { message: unknown }).message);
      }
      if (typeof err.error === 'string' && err.error) {
        try {
          const parsed = JSON.parse(err.error);
          if (parsed?.message) return String(parsed.message);
        } catch {
          /* ignore */
        }
      }
      if (err.message) return err.message;
    }
    return '请求失败，请稍后重试';
  }

  private handleError(err: HttpErrorResponse): Observable<never> {
    this.message.error(HttpApiService.extractMessage(err));
    return throwError(() => err);
  }
}
