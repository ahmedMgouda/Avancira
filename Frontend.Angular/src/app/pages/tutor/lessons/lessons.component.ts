import { CommonModule } from '@angular/common';
import { Component, OnInit, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';
import { DatePickerModule, DateRangePickerModule } from '@syncfusion/ej2-angular-calendars';
import { DropDownListModule } from '@syncfusion/ej2-angular-dropdowns';
import {
    DataStateChangeEventArgs,
    FilterService,
    GridComponent,
    GridModule,
    PageService,
    PageSettingsModel,
    ResizeService,
    SortService,
    ToolbarService
} from '@syncfusion/ej2-angular-grids';
import { NumericTextBoxModule, TextBoxModule } from '@syncfusion/ej2-angular-inputs';

import { ConfirmationDialogService } from '../../../services/confirmation-dialog.service';
import { GridState, GridStateService } from '../../../services/grid-state.service.service';
import { JitsiService } from '../../../services/jitsi.service';
import { LessonService } from '../../../services/lesson.service';
import { UserService } from '../../../services/user.service';

import { LoadingService } from '@core/loading/loading.service';
import { ToastService } from '@core/toast/toast.service';

import { DurationPipe } from '../../../pipes/duration.pipe';

import { LessonStatus } from '../../../models/enums/lesson-status';
import { LessonType } from '../../../models/enums/lesson-type';
import { UserRole } from '../../../models/enums/user-role';
import { Lesson } from '../../../models/lesson';
import { LessonFilter } from '../../../models/lesson-filter';

@Component({
    selector: 'app-lessons',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        GridModule,
        DatePickerModule,
        DateRangePickerModule,
        NumericTextBoxModule,
        DropDownListModule,
        TextBoxModule,
        ButtonModule,
        DurationPipe
    ],
    providers: [ToolbarService, PageService, SortService, FilterService, ResizeService],
    templateUrl: './lessons.component.html',
    styleUrls: ['./lessons.component.scss']
})

export class LessonsComponent implements OnInit {
    @ViewChild('grid') grid!: GridComponent;

    public gridData: { result: Lesson[]; count: number } = { result: [], count: 0 };
    public pageSettings: PageSettingsModel = { pageSize: 10, pageSizes: [5, 10, 20, 50, 100] };
    lessonFilter: LessonFilter = {
        status: -1
    };

    public LessonStatus = LessonStatus;
    public LessonType = LessonType;
    public UserRole = UserRole;


    //#region Internal State
    private currentPage = 1;
    private sortField?: keyof Lesson;
    private sortDirection: string = 'Ascending';
    //#endregion


    //#region Constructor & Lifecycle Hooks
    constructor(
        private loader: LoadingService,
        private toastService: ToastService,
        private lessonService: LessonService,
        private userService: UserService,
        private gridStateService: GridStateService,
        private confirmationDialogService: ConfirmationDialogService,
        private jitsiService: JitsiService,
    ) { }


    ngOnInit(): void {
        this.toastService.success('Lesson canceled successfully.');

        this.loadLessons();
    }

    onGridCreated(): void {
        if (this.grid) {
            setTimeout(() => {
                this.grid.pageSettings.pageSize = this.pageSettings.pageSize;
                this.grid.dataBind();
            }, 0);
        }
    }

    statusList = [
        { text: "All", value: -1 },
        ...Object.keys(LessonStatus)
            .filter(key => isNaN(Number(key)))
            .map(key => ({
                text: key,
                value: LessonStatus[key as keyof typeof LessonStatus]
            }))
    ];

    onDataStateChange(state: DataStateChangeEventArgs): void {
        const gridState: GridState<Lesson> = this.gridStateService.updateState<Lesson>(state);
        this.currentPage = gridState.currentPage;
        this.pageSettings.pageSize = gridState.pageSize;
        this.sortField = gridState.sortField;
        this.sortDirection = gridState.sortDirection || 'Ascending';
         this.loadLessons();
    }

    loadLessons(event?: any): void {
        if (event && event.isInteracted === false) return;

        this.loader.showGlobal('Loading lessons...');
        this.lessonService.getAllLessons(this.currentPage, this.pageSettings.pageSize, this.lessonFilter)
            .pipe(finalize(() => this.loader.hideGlobal()))
            .subscribe({
                next: (response) => {
                    this.gridData = { result: response.lessons.results, count: response.lessons.totalResults };
                },
                error: (err) => {
                    console.error('Failed to fetch lessons:', err);
                    this.toastService.error('Failed to load lessons. Please try again.');
                }
            });
    }

    async cancelLesson(lesson: Lesson) {
        const confirmed = await this.confirmationDialogService.confirm(
            'Are you sure you want to cancel this lesson?',
            'Cancel Lesson',
            'Yes',
            'No'
        );

        if (!confirmed) return;

        this.loader.showGlobal('Cancelling lesson...');

        this.lessonService.cancelLesson(lesson.id).pipe(finalize(() => this.loader.hideGlobal()))
            .subscribe({
                next: () => {
                    lesson.status = LessonStatus.Canceled;
                    this.grid.refresh();
                    this.toastService.success('Lesson canceled successfully.');
                },
                error: (err) => {
                    console.error('Failed to cancel lesson:', err);
                    this.toastService.error('Failed to cancel lesson. Please try again.');
                },
            });
    }

    async respondToProposition(lesson: Lesson, accept: boolean) {
        if (!accept) {
            const confirmed = await this.confirmationDialogService.confirm(
                'Are you sure you want to cancel this lesson?',
                'Cancel Lesson',
                'Yes',
                'No'
            );
            if (!confirmed) return;
        }

        this.loader.showGlobal('Updating lesson status...');

        this.lessonService.respondToProposition(lesson.id, accept).pipe(finalize(() => this.loader.hideGlobal()))
            .subscribe({
                next: () => {
                    lesson.status = accept ? LessonStatus.Booked : LessonStatus.Canceled;
                    this.grid.refresh();
                    const message = accept ? 'Lesson accepted successfully.' : 'Lesson canceled successfully.';
                    this.toastService.success(message);
                },
                error: (err) => {
                    console.error('Failed to respond to proposition:', err);
                    this.toastService.error('Failed to respond to proposition. Please try again.');
                }
            });
    }

    startCall(lesson: Lesson) {
        this.userService.getUser().subscribe({
            next: (user) => {
                this.jitsiService.startVideoCall(lesson, user);
            },
            error: (err) => console.error('Failed to fetch user:', err)
        });
    }

    getBadgeClass(status: LessonStatus): string {
        switch (status) {
            case LessonStatus.Proposed:
                return 'badge-Proposed';
            case LessonStatus.Booked:
                return 'badge-Booked';
            case LessonStatus.Completed:
                return 'badge-Completed';
            case LessonStatus.Canceled:
                return 'badge-Canceled';
            default:
                return '';
        }
    }
}

