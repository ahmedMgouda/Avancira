import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { HelpAddOrModifyMyPhoneNumberComponent } from './students/help-add-or-modify-my-phone-number/help-add-or-modify-my-phone-number.component';
import { HelpBenefitsOfTheStudentPassComponent } from './students/help-benefits-of-the-student-pass/help-benefits-of-the-student-pass.component';
import { HelpBookALessonComponent } from './students/help-book-a-lesson/help-book-a-lesson.component';
import { HelpBookAPackComponent } from './students/help-book-a-pack/help-book-a-pack.component';
import { HelpCancelALessonComponent } from './students/help-cancel-a-lesson/help-cancel-a-lesson.component';
import { HelpCancelAPackComponent } from './students/help-cancel-a-pack/help-cancel-a-pack.component';
import { HelpChangeMyEmailAddressComponent } from './students/help-change-my-email-address/help-change-my-email-address.component';
import { HelpChangeMyPaymentMethodComponent } from './students/help-change-my-payment-method/help-change-my-payment-method.component';
import { HelpChangeOrResetMyPasswordComponent } from './students/help-change-or-reset-my-password/help-change-or-reset-my-password.component';
import { HelpContactATutorComponent } from './students/help-contact-a-tutor/help-contact-a-tutor.component';
import { HelpCreateAnAccountComponent } from './students/help-create-an-account/help-create-an-account.component';
import { HelpDeleteMyStudentAccountComponent } from './students/help-delete-my-student-account/help-delete-my-student-account.component';
import { HelpEditALessonComponent } from './students/help-edit-a-lesson/help-edit-a-lesson.component';
import { HelpFaceToFaceOrWebcamLessonsComponent } from './students/help-face-to-face-or-webcam-lessons/help-face-to-face-or-webcam-lessons.component';
import { HelpFindATutorComponent } from './students/help-find-a-tutor/help-find-a-tutor.component';
import { HelpFirstLessonFreeComponent } from './students/help-first-lesson-free/help-first-lesson-free.component';
import { HelpInvoicesComponent } from './students/help-invoices/help-invoices.component';
import { HelpLeaveAReviewComponent } from './students/help-leave-a-review/help-leave-a-review.component';
import { HelpManageMyNotificationsComponent } from './students/help-manage-my-notifications/help-manage-my-notifications.component';
import { HelpManageMyStudentPassComponent } from './students/help-manage-my-student-pass/help-manage-my-student-pass.component';
import { HelpMethodOfPaymentComponent } from './students/help-method-of-payment/help-method-of-payment.component';
import { HelpPaymentForLessonsComponent } from './students/help-payment-for-lessons/help-payment-for-lessons.component';
import { HelpRefundComponent } from './students/help-refund/help-refund.component';
import { HelpReportALessonComponent } from './students/help-report-a-lesson/help-report-a-lesson.component';
import { HelpRequestAcceptedComponent } from './students/help-request-accepted/help-request-accepted.component';
import { HelpRequestRefusedComponent } from './students/help-request-refused/help-request-refused.component';
import { HelpRequestSentComponent } from './students/help-request-sent/help-request-sent.component';
import { HelpStartAVideoClassComponent } from './students/help-start-a-video-class/help-start-a-video-class.component';
import { HelpStudentCancellationPolicyComponent } from './students/help-student-cancellation-policy/help-student-cancellation-policy.component';
import { HelpSubscribeToTheStudentPassToContactATutorComponent } from './students/help-subscribe-to-the-student-pass-to-contact-a-tutor/help-subscribe-to-the-student-pass-to-contact-a-tutor.component';
import { HelpVideoLessonTechnicalIssuesComponent } from './students/help-video-lesson-technical-issues/help-video-lesson-technical-issues.component';
import { HelpCreateAnAdComponent } from './tutors/become-a-avancira/help-create-an-ad/help-create-an-ad.component';
import { HelpTheAvanciraGuidelinesComponent } from './tutors/become-a-avancira/help-the-avancira-guidelines/help-the-avancira-guidelines.component';
import { HelpDeactivateOrReactivateMyAdComponent } from './tutors/manage-my-ad/help-deactivate-or-reactivate-my-ad/help-deactivate-or-reactivate-my-ad.component';
import { HelpMyAdsVisibilityComponent } from './tutors/manage-my-ad/help-my-ads-visibility/help-my-ads-visibility.component';
import { HelpUpdateMyAdComponent } from './tutors/manage-my-ad/help-update-my-ad/help-update-my-ad.component';
import { HelpAcceptOrRefuseARequestComponent } from './tutors/manage-my-lesson-requests/help-accept-or-refuse-a-request/help-accept-or-refuse-a-request.component';
import { HelpLeaveOrRequestAReviewComponent } from './tutors/manage-my-lesson-requests/help-leave-or-request-a-review/help-leave-or-request-a-review.component';
import { HelpReceiveARecommendationComponent } from './tutors/manage-my-lesson-requests/help-receive-a-recommendation/help-receive-a-recommendation.component';
import { HelpReportAStudentComponent } from './tutors/manage-my-lesson-requests/help-report-a-student/help-report-a-student.component';
import { HelpCancellationPolicyComponent } from './tutors/manage-my-lessons/help-cancellation-policy/help-cancellation-policy.component';
import { HelpCancellingALessonPackComponent } from './tutors/manage-my-lessons/help-cancelling-a-lesson-pack/help-cancelling-a-lesson-pack.component';
import { HelpModifyOrCancelALessonComponent } from './tutors/manage-my-lessons/help-modify-or-cancel-a-lesson/help-modify-or-cancel-a-lesson.component';
import { HelpOrganizeAVideoLessonComponent } from './tutors/manage-my-lessons/help-organize-a-video-lesson/help-organize-a-video-lesson.component';
import { HelpScheduleALessonWithMyStudentComponent } from './tutors/manage-my-lessons/help-schedule-a-lesson-with-my-student/help-schedule-a-lesson-with-my-student.component';
import { HelpScheduleAPackComponent } from './tutors/manage-my-lessons/help-schedule-a-pack/help-schedule-a-pack.component';
import { HelpTutorsFirstLessonFreeComponent } from './tutors/manage-my-lessons/help-tutors-first-lesson-free/help-tutors-first-lesson-free.component';
import { HelpTutorsReportALessonComponent } from './tutors/manage-my-lessons/help-tutors-report-a-lesson/help-tutors-report-a-lesson.component';
import { HelpWhatIsYourStatusAsATutorComponent } from './tutors/manage-my-lessons/help-what-is-your-status-as-a-tutor/help-what-is-your-status-as-a-tutor.component';
import { HelpManageMySubscriptionComponent } from './tutors/premium-subscription/help-manage-my-subscription/help-manage-my-subscription.component';
import { HelpPremiumClubComponent } from './tutors/premium-subscription/help-premium-club/help-premium-club.component';
import { HelpChangeMyPayoutPreferenceComponent } from './tutors/tutor-payment/help-change-my-payout-preference/help-change-my-payout-preference.component';
import { HelpHowToGetPaidComponent } from './tutors/tutor-payment/help-how-to-get-paid/help-how-to-get-paid.component';
import { HelpLessonPackPaymentTimelinesComponent } from './tutors/tutor-payment/help-lesson-pack-payment-timelines/help-lesson-pack-payment-timelines.component';
import { HelpPaymentReceiptsComponent } from './tutors/tutor-payment/help-payment-receipts/help-payment-receipts.component';
import { HelpReceiveMyPaymentComponent } from './tutors/tutor-payment/help-receive-my-payment/help-receive-my-payment.component';

@Component({
  selector: 'app-help-center',
  imports: [
    CommonModule,
    FormsModule,

    // Student Components
    HelpFindATutorComponent,
    HelpContactATutorComponent,
    HelpRequestSentComponent,
    HelpRequestAcceptedComponent,
    HelpRequestRefusedComponent,
    HelpLeaveAReviewComponent,
    HelpFirstLessonFreeComponent,
    HelpFaceToFaceOrWebcamLessonsComponent,
    HelpStartAVideoClassComponent,
    HelpVideoLessonTechnicalIssuesComponent,
    HelpBookALessonComponent,
    HelpEditALessonComponent,
    HelpCancelALessonComponent,
    HelpBookAPackComponent,
    HelpCancelAPackComponent,
    HelpStudentCancellationPolicyComponent,
    HelpPaymentForLessonsComponent,
    HelpReportALessonComponent,
    HelpMethodOfPaymentComponent,
    HelpChangeMyPaymentMethodComponent,
    HelpRefundComponent,
    HelpInvoicesComponent,
    HelpCreateAnAccountComponent,
    HelpChangeOrResetMyPasswordComponent,
    HelpChangeMyEmailAddressComponent,
    HelpAddOrModifyMyPhoneNumberComponent,
    HelpManageMyNotificationsComponent,
    HelpSubscribeToTheStudentPassToContactATutorComponent,
    HelpBenefitsOfTheStudentPassComponent,
    HelpManageMyStudentPassComponent,
    HelpDeleteMyStudentAccountComponent,

    // Tutor Components
    HelpCreateAnAdComponent,
    HelpTheAvanciraGuidelinesComponent,
    HelpUpdateMyAdComponent,
    HelpMyAdsVisibilityComponent,
    HelpDeactivateOrReactivateMyAdComponent,
    HelpAcceptOrRefuseARequestComponent,
    HelpLeaveOrRequestAReviewComponent,
    HelpReceiveARecommendationComponent,
    HelpReportALessonComponent,
    HelpReportAStudentComponent,
    HelpTutorsReportALessonComponent,
    HelpTutorsFirstLessonFreeComponent,
    HelpScheduleALessonWithMyStudentComponent,
    HelpScheduleAPackComponent,
    HelpCancellingALessonPackComponent,
    HelpCancellationPolicyComponent,
    HelpWhatIsYourStatusAsATutorComponent,
    HelpModifyOrCancelALessonComponent,
    HelpOrganizeAVideoLessonComponent,
    HelpHowToGetPaidComponent,
    HelpReceiveMyPaymentComponent,
    HelpChangeMyPayoutPreferenceComponent,
    HelpLessonPackPaymentTimelinesComponent,
    HelpPaymentReceiptsComponent,
    HelpPremiumClubComponent,
    HelpManageMySubscriptionComponent
  ],
  templateUrl: './help-center.component.html',
  styleUrl: './help-center.component.scss'
})
export class HelpCenterComponent implements OnInit {
  categories: {
    students: { title: string; subcategories: string[] }[];
    tutors: { title: string; subcategories: string[] }[];
  } = {
      students: [
        {
          title: 'Find a tutor',
          subcategories: ['Find a tutor', 'Contact a tutor', 'Request sent']
        },
        {
          title: 'Manage my requests',
          subcategories: ['Request accepted', 'Request refused', 'Leave a review']
        },
        {
          title: 'Lesson management',
          subcategories: [
            '1st lesson free',
            'Face-to-face or webcam lessons',
            'Start a video class',
            'Video lesson technical issues',
            'Book a lesson',
            'Edit a lesson',
            'Cancel a lesson',
            'Book a pack',
            'Cancel a pack',
            'Student cancellation policy',
            'Payment for lessons',
            'Report a lesson'
          ]
        },
        {
          title: 'Payment',
          subcategories: ['Method of payment', 'Change my payment method', 'Refund', 'Invoices']
        },
        {
          title: 'My account',
          subcategories: [
            'Create an account',
            'Change or reset my password',
            'Change my email address',
            'Add or modify my phone number',
            'Manage my notifications'
          ]
        },
        {
          title: 'Student Pass subscription',
          subcategories: [
            'Subscribe to the Student Pass to contact a tutor',
            'Benefits of the Student Pass',
            'Manage my Student Pass',
            'Delete my student account'
          ]
        }],
      tutors: [
        {
          title: 'Become a Avancira',
          subcategories: ['Create an ad', 'The Avancira Guidelines']
        },
        {
          title: 'Manage my ad',
          subcategories: ['Update my ad', 'My ad\'s visibility', 'Deactivate or reactivate my ad']
        },
        {
          title: 'Manage my lesson requests',
          subcategories: [
            'Accept or refuse a request',
            'Leave or request a review',
            'Receive a recommendation',
            'Report a student'
          ]
        },
        {
          title: 'Manage my lessons',
          subcategories: [
            '1st lesson free',
            'Schedule a lesson with my student',
            'Schedule a pack',
            'Cancelling a lesson pack',
            'Cancellation Policy',
            'Report a lesson',
            'What is your status as a tutor?',
            'Modify or cancel a lesson',
            'Organize a video lesson'
          ]
        },
        {
          title: 'Tutor payment',
          subcategories: [
            'How to get paid?',
            'Receive my payment',
            'Change my payout preference',
            'Lesson / pack payment timelines',
            'Payment Receipts'
          ]
        },
        {
          title: 'Premium subscription',
          subcategories: ['Premium Club', 'Manage my subscription']
        }
      ]
    };

  // Restrict currentView to keys of the categories object
  currentView: keyof typeof this.categories = 'students';
  expandedCategory: any;
  selectedSubcategory: string = '';

  ngOnInit(): void {
    this.initializeCategories();
  }

  initializeCategories(): void {
    const categories = this.getCategories();
    this.expandedCategory = categories[0];
    this.selectedSubcategory = categories[0]?.subcategories[0];
  }

  getCategories(): { title: string; subcategories: string[] }[] {
    return this.categories[this.currentView];
  }

  toggleCategory(category: any): void {
    this.expandedCategory = this.expandedCategory === category ? null : category;
    this.selectedSubcategory = category.subcategories[0];
  }

  selectSubcategory(subcategory: string): void {
    this.selectedSubcategory = subcategory;
  }

  getRelatedArticles(): string[] {
    return this.expandedCategory?.subcategories.filter((sub: string) => sub !== this.selectedSubcategory) || [];
  }

  // Switch between students and tutors view
  switchView(view: 'students' | 'tutors'): void {
    this.currentView = view;
    this.initializeCategories();
  }
}
