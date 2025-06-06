import { Routes } from '@angular/router';

import { VideoCallWindowComponent } from './components/video-call-window/video-call-window.component';
import { FooterLayoutComponent } from './layout/footer-layout/footer-layout.component';
import { HeaderLayoutComponent } from './layout/header-layout/header-layout.component';
import { SidebarLayoutComponent } from './layout/sidebar-layout/sidebar-layout.component';
import { CompleteRegistrationComponent } from './pages/complete-registration/complete-registration.component';
import { ConfirmEmailComponent } from './pages/confirm-email/confirm-email.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { EvaluationsComponent } from './pages/evaluations/evaluations.component';
import { ForgotPasswordComponent } from './pages/forgot-password/forgot-password.component';
import { GoodbyeComponent } from './pages/goodbye/goodbye.component';
import { HomeComponent } from './pages/home/home.component';
import { InvoicesComponent } from './pages/invoices/invoices.component';
import { LessonsComponent } from './pages/lessons/lessons.component';
import { ListingsComponent } from './pages/listings/listings.component';
import { MessagesComponent } from './pages/messages/messages.component';
import { PaymentsComponent } from './pages/payments/payments.component';
import { PrivacyComponent } from './pages/privacy/privacy.component';
import { ProfileComponent } from './pages/profile/profile.component';
import { ResetPasswordComponent } from './pages/reset-password/reset-password.component';
import { SigninComponent } from './pages/signin/signin.component';
import { SignupComponent } from './pages/signup/signup.component';
import { TabletestComponent } from './pages/tabletest/tabletest.component';
import { TermsComponent } from './pages/terms/terms.component';

import { AuthGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: 'video-call-window', component: VideoCallWindowComponent },
  { path: 'signup', component: SignupComponent },
  { path: 'signin', component: SigninComponent },
  { path: 'forget-password', component: ForgotPasswordComponent },
  { path: 'reset-password', component: ResetPasswordComponent },
  { path: 'confirm-email', component: ConfirmEmailComponent },
  { path: 'complete-registration', component: CompleteRegistrationComponent },

  {
    path: '',
    component: HeaderLayoutComponent,
    children: [
      {
        path: '',
        component: FooterLayoutComponent, 
        children: [
          { path: '', component: HomeComponent },
          { path: 'terms', component: TermsComponent },
          { path: 'privacy-policy', component: PrivacyComponent },
          { path: 'goodbye', component: GoodbyeComponent },
      
          { path: 'search-results', loadComponent: () => import('./pages/search-results/search-results.component').then(m => m.SearchResultsComponent) },
          { path: 'category/:name', loadComponent: () => import('./components/categories/categories.component').then(m => m.CategoriesComponent) },
          { path: 'about', loadComponent: () => import('./pages/about-us/about-us.component').then(m => m.AboutUsComponent) },
          { path: 'states', loadComponent: () => import('./pages/states/states.component').then(m => m.StatesComponent) },
          { path: 'careers', loadComponent: () => import('./pages/careers/careers.component').then(m => m.CareersComponent) },
          { path: 'online-courses', loadComponent: () => import('./pages/online-courses/online-courses.component').then(m => m.OnlineCoursesComponent) },
          { path: 'help-centre', loadComponent: () => import('./pages/help-center/help-center.component').then(m => m.HelpCenterComponent) },
          { path: 'payment', loadComponent: () => import('./pages/payment/payment.component').then(m => m.PaymentComponent), canActivate: [AuthGuard] },
          { path: 'payment-result', loadComponent: () => import('./pages/payment-result/payment-result.component').then(m => m.PaymentResultComponent), canActivate: [AuthGuard] },
          { path: 'listing/:id', loadComponent: () => import('./pages/listing/listing.component').then(m => m.ListingComponent) },
          { path: 'booking/:id', loadComponent: () => import('./pages/booking/booking.component').then(m => m.BookingComponent) },
          { path: 'recommendation/:tokenId', loadComponent: () => import('./pages/recommendation-submission/recommendation-submission.component').then(m => m.RecommendationSubmissionComponent), canActivate: [AuthGuard] },
          { path: 'premium', loadComponent: () => import('./pages/premium/premium.component').then(m => m.PremiumComponent), canActivate: [AuthGuard] },
          { path: 'subscribe-premium', loadComponent: () => import('./pages/premium-subscription/premium-subscription.component').then(m => m.PremiumSubscriptionComponent), canActivate: [AuthGuard] }
        ]
      },    
      // Dashboard Routes
      {
        path: '',
        component: FooterLayoutComponent,
        children: [
          {
            path: 'dashboard',
            component: SidebarLayoutComponent,
            canActivate: [AuthGuard],
            children: [
              { path: 'tabletest', component: TabletestComponent, data: { title: 'tabletest' } },
              { path: '', component: DashboardComponent, data: { title: 'Dashboard' } },
              { path: 'listings', component: ListingsComponent, data: { title: 'Listings' } },
              { path: 'lessons', component: LessonsComponent, data: { title: 'Lessons' } },
              { path: 'evaluations', component: EvaluationsComponent, data: { title: 'Evaluations' } },
              { path: 'payments', component: PaymentsComponent, data: { title: 'Payments' } },
              { path: 'invoices', component: InvoicesComponent, data: { title: 'Invoices' } },
              { path: 'profile', component: ProfileComponent, data: { title: 'Profile' } },
            ]
          }
        ]
      },
      {
        path: 'messages',
        component: MessagesComponent,
        canActivate: [AuthGuard]
      }
    ]
  },
  
]
