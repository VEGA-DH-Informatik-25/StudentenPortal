import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { TranslationKey } from '../../../core/i18n/translations';

type LegalPageKind = 'impressum' | 'datenschutz' | 'nutzungsordnung';

interface LegalSection {
  title: TranslationKey;
  items: TranslationKey[];
}

interface LegalPageContent {
  title: TranslationKey;
  eyebrow: TranslationKey;
  body: TranslationKey;
  sections: LegalSection[];
}

const LEGAL_PAGES: Record<LegalPageKind, LegalPageContent> = {
  impressum: {
    title: 'legal.impressum.title',
    eyebrow: 'legal.impressum.eyebrow',
    body: 'legal.impressum.body',
    sections: [
      {
        title: 'legal.section.requiredDetails',
        items: [
          'legal.impressum.todo.responsible',
          'legal.impressum.todo.address',
          'legal.impressum.todo.contact',
          'legal.impressum.todo.representative',
        ],
      },
      {
        title: 'legal.section.projectContext',
        items: [
          'legal.impressum.context.project',
          'legal.impressum.context.repository',
          'legal.impressum.context.status',
        ],
      },
    ],
  },
  datenschutz: {
    title: 'legal.privacy.title',
    eyebrow: 'legal.privacy.eyebrow',
    body: 'legal.privacy.body',
    sections: [
      {
        title: 'legal.section.requiredDetails',
        items: [
          'legal.privacy.todo.controller',
          'legal.privacy.todo.contact',
          'legal.privacy.todo.hosting',
          'legal.privacy.todo.legalBasis',
        ],
      },
      {
        title: 'legal.section.dataProcessing',
        items: [
          'legal.privacy.data.accounts',
          'legal.privacy.data.content',
          'legal.privacy.data.external',
          'legal.privacy.data.retention',
        ],
      },
    ],
  },
  nutzungsordnung: {
    title: 'legal.usage.title',
    eyebrow: 'legal.usage.eyebrow',
    body: 'legal.usage.body',
    sections: [
      {
        title: 'legal.section.rules',
        items: [
          'legal.usage.rule.accounts',
          'legal.usage.rule.content',
          'legal.usage.rule.moderation',
          'legal.usage.rule.data',
        ],
      },
      {
        title: 'legal.section.requiredDetails',
        items: [
          'legal.usage.todo.scope',
          'legal.usage.todo.support',
          'legal.usage.todo.approval',
        ],
      },
    ],
  },
};

@Component({
  selector: 'app-legal-page',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './legal-page.html',
  styleUrl: './legal-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LegalPage {
  private readonly _route = inject(ActivatedRoute);

  protected readonly _links: ReadonlyArray<{ kind: LegalPageKind; href: string; label: TranslationKey }> = [
    { kind: 'impressum', href: '/legal/impressum', label: 'legal.impressum.link' },
    { kind: 'datenschutz', href: '/legal/datenschutz', label: 'legal.privacy.link' },
    { kind: 'nutzungsordnung', href: '/legal/nutzungsordnung', label: 'legal.usage.link' },
  ];

  protected get pageKind(): LegalPageKind {
    const legalPage = this._route.snapshot.data['legalPage'];
    return legalPage === 'datenschutz' || legalPage === 'nutzungsordnung' ? legalPage : 'impressum';
  }

  protected get content(): LegalPageContent {
    return LEGAL_PAGES[this.pageKind];
  }
}
