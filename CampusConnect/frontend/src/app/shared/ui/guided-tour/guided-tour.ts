import { AfterViewChecked, ChangeDetectionStrategy, Component, HostListener, inject, signal } from '@angular/core';
import { I18n } from '../../../core/i18n/i18n';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { GuidedTour } from '../../../core/services/guided-tour';

@Component({ selector: 'app-guided-tour', standalone: true, imports: [TranslatePipe], templateUrl: './guided-tour.html', styleUrl: './guided-tour.scss', changeDetection: ChangeDetectionStrategy.OnPush })
export class GuidedTourComponent implements AfterViewChecked {
  protected readonly tour = inject(GuidedTour);
  protected readonly i18n = inject(I18n);
  private _lastSelector = '';
  private _lastStepIndex = -1;
  private _target: HTMLElement | null = null;
  protected readonly tooltipLeft = signal(16);
  protected readonly tooltipTop = signal(16);
  protected readonly arrowStartX = signal(0);
  protected readonly arrowStartY = signal(0);
  protected readonly arrowEndX = signal(0);
  protected readonly arrowEndY = signal(0);

  ngAfterViewChecked(): void {
    const selector = this.tour.active() ? this.tour.steps[this.tour.stepIndex()]?.selector : '';
    const stepIndex = this.tour.stepIndex();
    if (selector === this._lastSelector && stepIndex === this._lastStepIndex && this._target) return;
    const target = selector ? document.querySelector<HTMLElement>(selector) : null;
    if (!target) {
      if (!this.tour.active()) this._clearTargetHighlight();
      return;
    }
    this._clearTargetHighlight();
    target.classList.add('tour-target');
    const step = this.tour.steps[this.tour.stepIndex()];
    if (step?.clickRequired || step?.inputRequired) target.classList.add('tour-target--interactive');
    target.scrollIntoView({ behavior: 'smooth', block: 'center' });
    this._lastSelector = selector;
    this._lastStepIndex = stepIndex;
    this._target = target;
    requestAnimationFrame(() => this._positionTooltip());
  }

  protected next(): void { this.tour.next(); }
  protected finish(): void { this.tour.finish(); }

  @HostListener('window:resize')
  @HostListener('window:scroll')
  protected reposition(): void { this._positionTooltip(); }

  @HostListener('document:click', ['$event.target'])
  protected advanceAfterRequiredClick(target: EventTarget | null): void {
    const step = this.tour.steps[this.tour.stepIndex()];
    if (!step?.clickRequired || !(target instanceof Element) || !target.closest(step.selector)) return;
    setTimeout(() => this.tour.next());
  }

  @HostListener('document:input', ['$event.target'])
  protected completeRequiredInput(target: EventTarget | null): void {
    const step = this.tour.steps[this.tour.stepIndex()];
    if (step?.inputRequired && target instanceof Element && target.closest(step.selector)) this.tour.completeInput();
  }

  private _positionTooltip(): void {
    const target = this._target;
    if (!target || !this.tour.active()) return;

    const rect = target.getBoundingClientRect();
    const padding = 16;
    const tooltipWidth = Math.min(360, window.innerWidth - padding * 2);
    const tooltipHeight = 190;
    const canPlaceRight = rect.right + tooltipWidth + padding <= window.innerWidth;
    const canPlaceLeft = rect.left - tooltipWidth - padding >= 0;
    const left = canPlaceRight
      ? rect.right + padding
      : canPlaceLeft
        ? rect.left - tooltipWidth - padding
        : Math.max(padding, Math.min(rect.left, window.innerWidth - tooltipWidth - padding));
    const prefersBelow = rect.bottom + tooltipHeight + padding <= window.innerHeight;
    const top = canPlaceRight || canPlaceLeft
      ? Math.max(padding, Math.min(rect.top + rect.height / 2 - tooltipHeight / 2, window.innerHeight - tooltipHeight - padding))
      : prefersBelow
        ? rect.bottom + padding
        : Math.max(padding, rect.top - tooltipHeight - padding);

    this.tooltipLeft.set(left);
    this.tooltipTop.set(top);
    this.arrowStartX.set(left < rect.left ? left + tooltipWidth : left);
    this.arrowStartY.set(top + 55);
    this.arrowEndX.set(rect.left + rect.width / 2);
    this.arrowEndY.set(rect.top + rect.height / 2);
  }

  private _clearTargetHighlight(): void {
    document.querySelectorAll<HTMLElement>('.tour-target').forEach((element) => {
      element.classList.remove('tour-target', 'tour-target--interactive');
    });
  }
}
