import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { LoadingService } from '../../services/loading.service';

const SKIP_LOADER_HEADER = 'X-Skip-Loader';

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);

  if (req.headers.has(SKIP_LOADER_HEADER)) {
    const cleanedReq = req.clone({
      headers: req.headers.delete(SKIP_LOADER_HEADER)
    });
    return next(cleanedReq);
  }

  loadingService.show();

  return next(req).pipe(
    finalize(() => loadingService.hide())
  );
};
