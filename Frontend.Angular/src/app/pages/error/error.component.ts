import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { environment } from '@/environments/environment';

@Component({
  selector: 'app-error',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="error-page">
      <div class="error-container">
        <div class="error-icon">⚠️</div>
        <h1>Something went wrong</h1>
        <p class="error-message">
          We encountered an unexpected error. Our team has been notified.
        </p>

        @if (correlationId) {
          <div class="correlation-id">
            <span class="label">Reference ID:</span>
            <code>{{ correlationId }}</code>
          </div>
        }

        <div class="error-actions">
          <button (click)="goHome()" class="btn-primary">
            Go to Home
          </button>
          <button (click)="reload()" class="btn-secondary">
            Reload Page
          </button>
        </div>

        @if (!production) {
          <details class="error-details">
            <summary>Technical Details (Development Only)</summary>
            <pre>{{ errorDetails | json }}</pre>
          </details>
        }
      </div>
    </div>
  `,
  styles: [`
    .error-page {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      padding: 2rem;
    }

    .error-container {
      background: white;
      border-radius: 1rem;
      padding: 3rem;
      max-width: 600px;
      text-align: center;
      box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
    }

    .error-icon {
      font-size: 4rem;
      margin-bottom: 1rem;
    }

    h1 {
      font-size: 2rem;
      color: #1f2937;
      margin-bottom: 1rem;
    }

    .error-message {
      color: #6b7280;
      font-size: 1.125rem;
      margin-bottom: 2rem;
    }

    .correlation-id {
      background: #f3f4f6;
      border-radius: 0.5rem;
      padding: 1rem;
      margin-bottom: 2rem;
    }

    .label {
      display: block;
      font-size: 0.875rem;
      color: #6b7280;
      margin-bottom: 0.5rem;
    }

    code {
      font-family: 'Courier New', monospace;
      font-size: 0.875rem;
      color: #4f46e5;
    }

    .error-actions {
      display: flex;
      gap: 1rem;
      justify-content: center;
      margin-bottom: 2rem;
    }

    button {
      padding: 0.75rem 1.5rem;
      border-radius: 0.5rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
      border: none;
    }

    .btn-primary {
      background: #4f46e5;
      color: white;
    }

    .btn-primary:hover {
      background: #4338ca;
      transform: translateY(-2px);
    }

    .btn-secondary {
      background: #f3f4f6;
      color: #1f2937;
    }

    .btn-secondary:hover {
      background: #e5e7eb;
    }

    .error-details {
      margin-top: 2rem;
      text-align: left;
    }

    summary {
      cursor: pointer;
      font-weight: 600;
      color: #4f46e5;
      margin-bottom: 1rem;
    }

    pre {
      background: #1f2937;
      color: #10b981;
      padding: 1rem;
      border-radius: 0.5rem;
      overflow-x: auto;
      font-size: 0.875rem;
    }
  `]
})
export class ErrorComponent implements OnInit {
  correlationId: string | null = null;
  errorCode: string | null = null;
  errorDetails: any = null;
  production = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.production = environment.production;
  }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.correlationId = params['correlationId'] || null;
      this.errorCode = params['code'] || null;
      
      // In development, show more details
      if (!this.production) {
        this.errorDetails = {
          correlationId: this.correlationId,
          errorCode: this.errorCode,
          timestamp: new Date().toISOString(),
          url: window.location.href
        };
      }
    });
  }

  goHome(): void {
    this.router.navigate(['/']);
  }

  reload(): void {
    window.location.reload();
  }
}