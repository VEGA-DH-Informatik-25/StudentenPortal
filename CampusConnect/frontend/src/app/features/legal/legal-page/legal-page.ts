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
  noticeTitle: TranslationKey;
  noticeBody: TranslationKey;
  sections: LegalSection[];
}

const LEGAL_PAGES: Record<LegalPageKind, LegalPageContent> = {
  impressum: {
    title: 'legal.impressum.title',
    eyebrow: 'legal.impressum.eyebrow',
    body: 'legal.impressum.body',
    noticeTitle: 'legal.impressum.noticeTitle',
    noticeBody: 'legal.impressum.noticeBody',
    sections: [
      {
        title: 'legal.impressum.section.provider',
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
    noticeTitle: 'legal.privacy.noticeTitle',
    noticeBody: 'legal.privacy.noticeBody',
    sections: [
      {
        title: 'legal.privacy.section.controller',
        items: [
          'legal.privacy.todo.controller',
          'legal.privacy.todo.contact',
          'legal.privacy.scope',
        ],
      },
      {
        title: 'legal.section.dataProcessing',
        items: [
          'legal.privacy.data.accounts',
          'legal.privacy.data.profile',
          'legal.privacy.data.content',
          'legal.privacy.data.study',
          'legal.privacy.data.technical',
        ],
      },
      {
        title: 'legal.privacy.section.operation',
        items: [
          'legal.privacy.todo.hosting',
          'legal.privacy.data.external',
          'legal.privacy.browserStorage',
          'legal.privacy.noTracking',
        ],
      },
      {
        title: 'legal.privacy.section.legalBasisRetention',
        items: [
          'legal.privacy.todo.legalBasis',
          'legal.privacy.data.retention',
        ],
      },
      {
        title: 'legal.privacy.section.rights',
        items: [
          'legal.privacy.rights',
          'legal.privacy.complaint',
        ],
      },
    ],
  },
  nutzungsordnung: {
    title: 'legal.usage.title',
    eyebrow: 'legal.usage.eyebrow',
    body: 'legal.usage.body',
    noticeTitle: 'legal.usage.noticeTitle',
    noticeBody: 'legal.usage.noticeBody',
    sections: [
      {
        title: 'legal.usage.section.scopeAccess',
        items: [
          'legal.usage.rule.scope',
          'legal.usage.rule.accounts',
          'legal.usage.rule.credentials',
        ],
      },
      {
        title: 'legal.usage.section.communication',
        items: [
          'legal.usage.rule.communicationScope',
          'legal.usage.rule.content',
          'legal.usage.rule.studyRelated',
        ],
      },
      {
        title: 'legal.usage.section.prohibited',
        items: [
          'legal.usage.rule.prohibitedHarassment',
          'legal.usage.rule.prohibitedSpam',
          'legal.usage.rule.prohibitedIllegal',
          'legal.usage.rule.prohibitedConfidential',
        ],
      },
      {
        title: 'legal.usage.section.moderation',
        items: [
          'legal.usage.rule.moderation',
          'legal.usage.rule.consequences',
          'legal.usage.rule.reporting',
        ],
      },
      {
        title: 'legal.usage.section.dataSecurity',
        items: [
          'legal.usage.rule.data',
          'legal.usage.rule.security',
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
