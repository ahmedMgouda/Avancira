import { Routes } from '@angular/router';

// ============================================
// Page Components
// ============================================
import { VideoCallWindowComponent } from './components/video-call-window/video-call-window.component';
import { AdminShellComponent } from './layouts/admin/admin-shell.component';
import { PortalShellComponent } from './layouts/portal/portal-shell.component';
import { BlankShellComponent } from './layouts/public/blank-shell.component';
// ============================================
// Layout Components
// ============================================
import { SiteShellComponent } from './layouts/public/site-shell.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { EvaluationsComponent } from './pages/evaluations/evaluations.component';
import { HomeComponent } from './pages/home/home.component';
import { InvoicesComponent } from './pages/invoices/invoices.component';
import { LessonsComponent } from './pages/lessons/lessons.component';
import { ListingsComponent } from './pages/listings/listings.component';
import { MessagesComponent } from './pages/messages/messages.component';
import { PaymentsComponent } from './pages/payments/payments.component';
import { PrivacyComponent } from './pages/privacy/privacy.component';
import { ProfileComponent } from './pages/profile/profile.component';
import { SessionsComponent } from './pages/sessions/sessions.component';
import { TabletestComponent } from './pages/tabletest/tabletest.component';
import { TermsComponent } from './pages/terms/terms.component';

import { adminChildGuard,adminGuard } from './guards/admin.guard';
// ============================================
// Guards
// ============================================
import { authGuard } from './guards/auth.guard';
import { studentChildGuard,studentGuard } from './guards/student.guard';
import { tutorChildGuard,tutorGuard } from './guards/tutor.guard';

export const routes: Routes = [
  // ============================================
  // BLANK LAYOUT (No Header/Footer)
  // ============================================
  {
    path: '',
    component: BlankShellComponent,
    children: [
      { path: 'video-call-window', component: VideoCallWindowComponent },
    ]
  },

  // ============================================
  // PUBLIC SITE LAYOUT (Marketing/Landing)
  // ============================================
  {
    path: '',
    component: SiteShellComponent,
    children: [
      { path: '', component: HomeComponent },
      { path: 'terms', component: TermsComponent },
      { path: 'privacy-policy', component: PrivacyComponent },
      
      // Lazy-loaded public pages
      { path: 'search-results', loadComponent: () => import('./pages/search-results/search-results.component').then(m => m.SearchResultsComponent) },
      { path: 'category/:name', loadComponent: () => import('./components/categories/categories.component').then(m => m.CategoriesComponent) },
      { path: 'about', loadComponent: () => import('./pages/about-us/about-us.component').then(m => m.AboutUsComponent) },
      { path: 'states', loadComponent: () => import('./pages/states/states.component').then(m => m.StatesComponent) },
      { path: 'careers', loadComponent: () => import('./pages/careers/careers.component').then(m => m.CareersComponent) },
      { path: 'online-courses', loadComponent: () => import('./pages/online-courses/online-courses.component').then(m => m.OnlineCoursesComponent) },
      { path: 'help-centre', loadComponent: () => import('./pages/help-center/help-center.component').then(m => m.HelpCenterComponent) },
      { path: 'listing/:id', loadComponent: () => import('./pages/listing/listing.component').then(m => m.ListingComponent) },

      // Generic authenticated pages (any logged-in user)
      { path: 'payment', loadComponent: () => import('./pages/payment/payment.component').then(m => m.PaymentComponent), canActivate: [authGuard] },
      { path: 'payment-result', loadComponent: () => import('./pages/payment-result/payment-result.component').then(m => m.PaymentResultComponent), canActivate: [authGuard] },
      { path: 'booking/:id', loadComponent: () => import('./pages/booking/booking.component').then(m => m.BookingComponent), canActivate: [authGuard] },
      { path: 'recommendation/:tokenId', loadComponent: () => import('./pages/recommendation-submission/recommendation-submission.component').then(m => m.RecommendationSubmissionComponent), canActivate: [authGuard] },
      { path: 'premium', loadComponent: () => import('./pages/premium/premium.component').then(m => m.PremiumComponent), canActivate: [authGuard] },
      { path: 'subscribe-premium', loadComponent: () => import('./pages/premium-subscription/premium-subscription.component').then(m => m.PremiumSubscriptionComponent), canActivate: [authGuard] },
    ]
  },

  // ============================================
  // STUDENT PORTAL (Portal Shell)
  // ============================================
  {
    path: 'student',
    component: PortalShellComponent,
    canActivate: [studentGuard],
    canActivateChild: [studentChildGuard],
    data: { role: 'student' }, // ← Portal shell uses this to show correct nav
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent, data: { title: 'Dashboard' } },
      { path: 'messages', component: MessagesComponent, data: { title: 'Messages' } },
      { path: 'lessons', component: LessonsComponent, data: { title: 'My Lessons' } },
      { path: 'evaluations', component: EvaluationsComponent, data: { title: 'My Reviews' } },
      { path: 'payments', component: PaymentsComponent, data: { title: 'Payments' } },
      { path: 'invoices', component: InvoicesComponent, data: { title: 'Invoices' } },
      { path: 'profile', component: ProfileComponent, data: { title: 'Profile Settings' } },
    ]
  },

  // ============================================
  // TUTOR PORTAL (Portal Shell)
  // ============================================
  {
    path: 'tutor',
    component: PortalShellComponent,
    canActivate: [tutorGuard],
    canActivateChild: [tutorChildGuard],
    data: { role: 'tutor' }, // ← Portal shell uses this to show correct nav
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent, data: { title: 'Dashboard' } },
      { path: 'messages', component: MessagesComponent, data: { title: 'Messages' } },
      { path: 'listings', component: ListingsComponent, data: { title: 'My Listings' } },
      { path: 'lessons', component: LessonsComponent, data: { title: 'Lessons' } },
      { path: 'evaluations', component: EvaluationsComponent, data: { title: 'Reviews' } },
      { path: 'payments', component: PaymentsComponent, data: { title: 'Payments' } },
      { path: 'invoices', component: InvoicesComponent, data: { title: 'Invoices' } },
      { path: 'sessions', component: SessionsComponent, data: { title: 'Sessions' } },
      { path: 'profile', component: ProfileComponent, data: { title: 'Profile Settings' } },
      { path: 'tabletest', component: TabletestComponent, data: { title: 'Table Test' } },
    ]
  },

  // ============================================
  // ADMIN PORTAL (Admin Shell)
  // ============================================
  {
    path: 'admin',
    component: AdminShellComponent,
    canActivate: [adminGuard],
    canActivateChild: [adminChildGuard],
    data: { role: 'admin' },
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent, data: { title: 'Admin Dashboard' } },
      // Add more admin-specific routes here
      // { path: 'users', component: AdminUsersComponent },
      // { path: 'reports', component: AdminReportsComponent },
      // { path: 'settings', component: AdminSettingsComponent },
    ]
  },

  // ============================================
  // Legacy Redirects
  // ============================================
  {
    path: 'dashboard',
    redirectTo: '/',
    pathMatch: 'full'
  },
  {
    path: 'dashboard/**',
    redirectTo: '/',
    pathMatch: 'full'
  },

  // ============================================
  // Fallback
  // ============================================
  { path: '**', redirectTo: '' }
];