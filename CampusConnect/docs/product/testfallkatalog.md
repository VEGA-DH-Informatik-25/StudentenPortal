# Testfallkatalog

Stand der Quelltextpruefung: 2026-06-29.

Dieser Katalog beschreibt die im Repository vorhandenen automatisierten Tests. Er ist aus den aktuellen Testdateien unter `backend/CampusConnect.API.Tests`, `backend/CampusConnect.Application.Tests`, `frontend/src/app/**/*.spec.ts` und `frontend/e2e` abgeleitet. Die Kommandoausgaben bleiben fuer Testzahlen und Testerfolg massgeblich.

## Backend API- und Integrationstests

Die API-Tests laufen gegen `WebApplicationFactory<Program>` oder gezielt konfigurierte Infrastrukturinstanzen. Sie pruefen HTTP-Statuscodes, JSON-Antworten, Authentifizierung, EF-Core-Persistenz, Migrationen, Demo-Seeding und externe Parser.

### `CampusConnect.API.Tests/ApiAuthorizationTests.cs`

- `ProtectedEndpoints_WithoutToken_ReturnUnauthorized`: prueft, dass geschuetzte GET-Endpunkte ohne Token `401 Unauthorized` liefern. Vorgehen: fuer `/api/auth/me`, `/api/feed`, `/api/groups/{id}/pending-posts`, `/api/groups`, `/api/grades`, `/api/calendar`, `/api/contacts`, `/api/timetable`, `/api/mensa` und `/api/admin/users` wird ein anonymer Client verwendet und der Statuscode verglichen.
- `CoursesEndpoint_AllowsAnonymousRequests`: prueft, dass `/api/courses` oeffentlich erreichbar ist und nur aktive, nicht-systemische Kurse enthaelt. Vorgehen: anonymer GET, JSON als Kursliste lesen, `TIF25A` erwarten und `ADMIN` ausschliessen.
- `DeleteGroup_WithoutToken_ReturnsUnauthorized`: prueft, dass Gruppenloeschung ohne Authentifizierung blockiert wird. Vorgehen: anonymer DELETE auf eine Beispielgruppen-ID und Status `401` erwarten.
- `SelfRegistrationEndpoint_IsNotAvailable`: prueft, dass oeffentliche Selbstregistrierung nicht existiert. Vorgehen: POST auf `/api/auth/register` mit gueltig wirkenden Nutzerdaten und `404 NotFound` erwarten.
- `AuthCookie_AllowsReloadedBrowserSessionUntilLogout`: prueft die HttpOnly-Cookie-Session nach Login und Logout. Vorgehen: Admin per `/api/auth/login` anmelden, `/api/auth/me` ohne Bearer-Header erfolgreich lesen, `/api/auth/logout` ausfuehren und anschliessend fuer `/api/auth/me` `401` erwarten.
- `AdminEndpoint_WithStudentToken_ReturnsForbidden`: prueft Rollenautorisierung fuer Admin-Endpunkte. Vorgehen: Student per Admin-Testclient anlegen, Student-JWT setzen, `/api/admin/users` aufrufen und `403 Forbidden` erwarten.
- `UpdateProfile_WithChangedCourse_ReturnsBadRequestAndKeepsAssignedCourse`: prueft, dass Nutzer ihren Kurs nicht selbst aendern koennen. Vorgehen: zweiten Kurs anlegen, Student einloggen, Profil-Update mit anderem Kurs senden, `400 BadRequest` erwarten und danach unveraenderte Profilfelder ueber `/api/auth/me` pruefen.
- `ProtectedEndpoint_WithTokenMissingUserId_ReturnsUnauthorized`: prueft Token-Validierung ohne User-ID-Claim. Vorgehen: Test-JWT ohne User-ID setzen, `/api/grades` aufrufen und `401` erwarten.
- `ProtectedEndpoint_WithTokenForUnknownUser_ReturnsUnauthorized`: prueft Token-Validierung fuer nicht vorhandene Nutzer. Vorgehen: JWT mit zufaelliger User-ID setzen, `/api/grades` aufrufen und `401` erwarten.
- `ProtectedEndpoint_WithTokenForInactiveUser_ReturnsUnauthorized`: prueft Token-Validierung fuer deaktivierte Nutzer. Vorgehen: inaktiven Nutzer anlegen, dessen JWT verwenden, `/api/grades` aufrufen und `401` erwarten.
- `GradesEndpoint_WithStudentToken_ReturnsCurrentUserSummary`: prueft erfolgreichen Zugriff eines Studenten auf eigene Noten. Vorgehen: Student anlegen, JWT setzen, `/api/grades` aufrufen und leere Notenliste sowie `TotalEcts = 0` pruefen.

### `CampusConnect.API.Tests/AdminUsersApiTests.cs`

- `AdminCanCreateAndUpdateUser`: prueft den Admin-Workflow fuer Nutzeranlage und -bearbeitung. Vorgehen: Admin-Client erstellt einen Nutzer, liest die Antwort, aktualisiert Profildaten/Rolle/Kurs ueber Admin-Endpunkte und vergleicht die gespeicherten Werte.
- `AdminCannotDeactivateSelf`: prueft Selbstschutz fuer Admin-Konten. Vorgehen: angemeldeter Admin versucht, den eigenen Status zu deaktivieren, und der Test erwartet eine abweisende Antwort statt erfolgreicher Deaktivierung.
- `AdminCanResetPasswordAndUserMustChangeItOnNextLogin`: prueft den Admin-Passwort-Reset. Vorgehen: Admin setzt ein neues Initialpasswort, altes Passwort wird abgewiesen, Login mit neuem Passwort liefert `mustChangePassword = true` und `onboardingCompleted = false`.

### `CampusConnect.API.Tests/AuthLoginRateLimitApiTests.cs`

- `Login_FifthFailedAttempt_ReturnsTooManyRequestsWithNeutralMessage`: prueft temporaere Login-Drosselung und neutrale Fehlermeldungen. Vorgehen: ein Client mit eindeutiger User-Agent-Kennung sendet vier falsche Logins mit `401`, der fuenfte Versuch muss `429 TooManyRequests` liefern und darf keine Account-Existenz verraten.
- `Login_SuccessfulAttemptAfterFailures_ResetsFailureCounters`: prueft Reset nach erfolgreichem Login. Vorgehen: vier falsche Admin-Logins senden, danach korrekt anmelden, anschliessend erneut falsch anmelden und nur `401` statt sofortigem `429` erwarten.

### `CampusConnect.API.Tests/DatabaseInitializerTests.cs`

- `InitializeAsync_ShouldEnsureConfiguredAdminCourseExistsAndIsActive`: prueft, dass der Initializer den konfigurierten Admin-Kurs anlegt oder aktiviert. Vorgehen: isolierte SQLite-Datenbank initialisieren und danach Kurscode/Aktivstatus kontrollieren.
- `InitializeAsync_ShouldEnsureConfiguredAdminCourseEvenWithoutAdminCredentials`: prueft, dass der Admin-Kurs auch ohne Bootstrap-Admin-Zugangsdaten vorhanden ist. Vorgehen: Initializer ohne Admin-Credentials ausfuehren und den Kurs in der Datenbank suchen.
- `FeedModerationMigration_ShouldPublishExistingPostsAndAllowComments`: prueft die Baseline/Migration fuer aeltere Feed-Daten. Vorgehen: alte Feed-Post-Struktur vorbereiten, Initialisierung/Migration laufen lassen und `Published` sowie erlaubte Kommentare fuer bestehende Posts erwarten.

### `CampusConnect.API.Tests/DevelopmentDemoDataSeederTests.cs`

- `SeedAsync_WhenEnabled_ShouldPopulateDevelopmentHubData`: prueft das Development-Demo-Seeding. Vorgehen: Seeding mit aktivierter Demo-Konfiguration in isolierter Datenbank ausfuehren und erwartete Kurse, Nutzer, Gruppen, Feed-, Noten- und Kalenderdaten validieren.

### `CampusConnect.API.Tests/DhbwTimetableServiceTests.cs`

- `GetTimetableAsync_UsesConfiguredTemplateAndCourseAlias`: prueft Kalender-URL-Erzeugung und Kursalias-Aufloesung. Vorgehen: Fake-HTTP-Antwort fuer iCal konfigurieren, Service mit URL-Template und Alias aufrufen und angefragte URL sowie geparste Termine vergleichen.
- `GetTimetableAsync_IncludesPastDaysFromCurrentWeek`: prueft, dass die Wochenansicht vergangene Tage der sichtbaren Woche einschliesst. Vorgehen: feste Uhrzeit setzen, iCal mit Terminen in der Woche liefern und Datumsbereich der Antwort pruefen.
- `GetTimetableAsync_UsesExplicitPastRangeStart`: prueft expliziten Startzeitpunkt fuer Ruecknavigation. Vorgehen: `from`-Datum in der Vergangenheit uebergeben und sicherstellen, dass Termine ab diesem sichtbaren Bereich erscheinen.
- `GetTimetableAsync_UsesFreshCachedIcalWithoutSecondRequest`: prueft Cache-Nutzung fuer frische iCal-Daten. Vorgehen: Service zweimal mit gleichem Kurs aufrufen und sicherstellen, dass der Fake-HTTP-Handler nur einmal angefragt wird.
- `GetTimetableAsync_ReturnsStaleCachedIcalWhenUpstreamFails`: prueft Fallback auf alten Cache bei Upstream-Fehlern. Vorgehen: erst erfolgreiche iCal-Antwort cachen, danach Fehler ausloesen und trotzdem geparste Termine aus dem Cache erwarten.
- `AddInfrastructure_ResolvesTimetableServiceTypedHttpClient`: prueft DI-Registrierung der Infrastruktur. Vorgehen: ServiceCollection mit Infrastruktur registrieren und `ITimetableService` als typed HTTP client erfolgreich aufloesen.

### `CampusConnect.API.Tests/EntityFeatureRepositoryTests.cs`

- `FeatureRepositories_ShouldPersistDataAcrossDbContextInstances`: prueft EF-Repositories fuer Feed, Gruppen, Noten und Exams ueber DbContext-Grenzen hinweg. Vorgehen: Daten in einem Context speichern, neuen Context oeffnen und persistierte Entitaeten samt JSON-Feldern wieder lesen.
- `EntityFeedRepository_ShouldReturnCloneInsteadOfTrackedPostReference`: prueft, dass Feed-Repository-Rueckgaben keine getrackten Live-Referenzen nach aussen reichen. Vorgehen: Post laden, Rueckgabe mutieren, erneut laden und unveraenderte gespeicherte Daten erwarten.

### `CampusConnect.API.Tests/EntityUserRepositoryTests.cs`

- `AddAndFindByEmailAsync_ShouldNormalizeEmailCaseAndWhitespace`: prueft E-Mail-Normalisierung im EF-User-Repository. Vorgehen: User mit Leerzeichen/Grossschreibung speichern und per normalisierter E-Mail wiederfinden.

### `CampusConnect.API.Tests/GroupModerationApiTests.cs`

- `GroupModerationWorkflow_ShouldPublishPendingPostAndDeleteGroup`: prueft Moderationsflow fuer Gruppenposts. Vorgehen: Owner und Mitglied anlegen, moderierte Gruppe erstellen, Mitglied hinzufuegen, Pending-Post erzeugen, Pending-Liste lesen, Post genehmigen, Sichtbarkeit im Feed pruefen, Loeschverbot fuer Mitglied und erfolgreiches Loeschen durch Owner pruefen.
- `LeaveGroup_ShouldRemoveMemberAndTransferOwner`: prueft Austritt und Owner-Transfer. Vorgehen: Gruppe erstellen, Mitglied hinzufuegen, normalen Mitgliedsaustritt pruefen, Mitglied erneut hinzufuegen, Owner mit `newOwnerUserId` austreten lassen und neue Ownership sowie entfernte Zuweisung des alten Owners validieren.
- `FeedAttachments_ShouldUploadAndRequireGroupReadAccess`: prueft Feed-Anhaenge und Leseberechtigung. Vorgehen: Gruppe mit Owner/Mitglied/Aussenstehendem erstellen, Multipart-Post mit Uebersetzungen und PDF hochladen, Metadaten pruefen, Download fuer Mitglied erlauben und fuer Aussenstehenden `403` erwarten.

### `CampusConnect.API.Tests/InMemoryFeedRepositoryTests.cs`

- `FindByIdAsync_ShouldReturnCloneInsteadOfStoredPostReference`: prueft Clone-Semantik des In-Memory-Feed-Repositories. Vorgehen: Post speichern, Rueckgabe mutieren, erneut laden und unveraenderten gespeicherten Post erwarten.
- `ToggleReactionAsync_ShouldReturnUpdatedCloneWithoutLeakingStoredReactionSet`: prueft, dass Reaktionsmengen nicht als mutable interne Referenz herausgegeben werden. Vorgehen: Reaktion toggeln, Rueckgabe manipulieren und erneutes Laden mit korrektem Repository-Zustand vergleichen.

### `CampusConnect.API.Tests/TimetableControllerTests.cs`

- `GetTimetable_WithoutCourseQuery_UsesCurrentUsersCourse`: prueft Controller-Fallback auf den Kurs des angemeldeten Nutzers. Vorgehen: Testnutzer mit Kurs bereitstellen, `/api/timetable` ohne Query-Kurs aufrufen und im Fake-Timetable-Service den verwendeten Kurs pruefen.
- `GetTimetable_ForwardsExplicitRangeStart`: prueft Weitergabe des `from`-Parameters. Vorgehen: `/api/timetable?from=...` aufrufen und sicherstellen, dass der Fake-Service das explizite Startdatum erhaelt.

## Backend Application-Tests

Die Application-Tests rufen Services direkt mit kleinen Fake-Repositories auf. Sie pruefen fachliche Regeln, Result-Fehler, Seiteneffekte und Security-Helfer ohne HTTP-Schicht.

### `CampusConnect.Application.Tests/Common/Security/PasswordHasherTests.cs`

- `Hash_ShouldUseUniqueSaltForEachPassword`: prueft Salt-Zufaelligkeit. Vorgehen: dasselbe Passwort mehrfach hashen und unterschiedliche Hashwerte erwarten.
- `Verify_ShouldRejectWrongPassword`: prueft Passwortverifikation gegen falsche Eingaben. Vorgehen: Hash fuer bekanntes Passwort erzeugen und Verifikation mit anderem Passwort ablehnen.
- `Verify_ShouldAcceptLegacySha256Hashes`: prueft Rueckwaertskompatibilitaet fuer alte SHA-256-Hashes. Vorgehen: Legacy-Hash vorbereiten und mit korrektem Passwort erfolgreich verifizieren.

### `Features/Admin/AdminUsersServiceTests.cs`

- `CreateUserAsync_CreatesUserWithSelectedCourseAndHashedPassword`: prueft Admin-Nutzeranlage. Vorgehen: Kurs und Fake-Repositories bereitstellen, Nutzer mit Initialpasswort anlegen, Kurszuordnung, Rolle, Hash statt Klartext und Gruppensynchronisierung pruefen.
- `CreateUserAsync_RejectsDuplicateEmail`: prueft E-Mail-Eindeutigkeit. Vorgehen: bestehenden Nutzer im Fake-Repository anlegen, neuen Nutzer mit gleicher E-Mail erstellen wollen und Fehlerresultat erwarten.
- `UpdateUserAsync_UpdatesProfileRoleAndCourse`: prueft Admin-Update fuer Profil, Rolle und Kurs. Vorgehen: Nutzer und Zielkurs vorbereiten, Update ausfuehren und gespeicherte Profildaten sowie Kurszuordnung pruefen.
- `UpdateUserAsync_PreventsCurrentAdminDemotingSelf`: prueft Selbstschutz gegen eigene Rollenherabstufung. Vorgehen: Admin aktualisiert eigenes Konto mit niedrigerer Rolle und der Service muss ablehnen.
- `UpdateStatusAsync_DeactivatesAndReactivatesUser`: prueft Statuswechsel. Vorgehen: Nutzer deaktivieren und wieder aktivieren, jeweils gespeicherten `IsActive`-Wert kontrollieren.
- `UpdateStatusAsync_PreventsCurrentAdminDeactivatingSelf`: prueft Selbstschutz im Status-Endpunkt. Vorgehen: aktueller Admin versucht, sich selbst zu deaktivieren, und der Service gibt Fehler zurueck.
- `UpdateUserAsync_PreventsCurrentAdminDeactivatingSelf`: prueft denselben Selbstschutz beim vollstaendigen Nutzerupdate. Vorgehen: eigenes Admin-Konto mit `IsActive = false` aktualisieren wollen und Ablehnung erwarten.
- `UpdateRoleAsync_AllowsAssigningManagementRole`: prueft Rollen-Patch fuer Management. Vorgehen: Nutzer vorbereiten, Rolle auf `Management` setzen und gespeicherte Rolle kontrollieren.
- `ResetPasswordAsync_SetsNewHashAndReopensInitialPasswordFlow`: prueft Passwort-Reset im Service. Vorgehen: Nutzer mit abgeschlossenem Onboarding vorbereiten, neues Initialpasswort setzen, neuen Hash sowie `MustChangePassword = true`, `OnboardingCompleted = false` und geloeschtes Abschlussdatum erwarten.

### `Features/Auth/AuthServiceTests.cs`

- `LoginAsync_AcceptsEmailWithDifferentCaseAndWhitespace`: prueft Login-Normalisierung. Vorgehen: Nutzer mit Kleinbuchstaben-E-Mail speichern, Login mit Leerzeichen/Grossschreibung senden und Erfolg mit normalisierter Profil-E-Mail erwarten.
- `LoginAsync_RejectsInactiveUser`: prueft Login-Sperre fuer deaktivierte Nutzer. Vorgehen: inaktiven Nutzer mit korrektem Passwort anlegen und neutrale Login-Fehlermeldung erwarten.
- `LoginAsync_UsesNeutralErrorForUnknownAccountAndWrongPassword`: prueft Account-Enumeration-Schutz und Failure-Tracking. Vorgehen: unbekannte E-Mail und falsches Passwort fuer bekannten Account testen, gleiche neutrale Meldung und zwei registrierte Fehlversuche erwarten.
- `LoginAsync_ReturnsRateLimitErrorWhenLimiterBlocks`: prueft vorgeschaltete Login-Drosselung. Vorgehen: Fake-Limiter blockiert bereits beim Check, Service gibt Rate-Limit-Fehler zurueck und zaehlt keinen weiteren Fehlversuch.
- `LoginAsync_ResetsRateLimiterAfterSuccessfulLogin`: prueft Reset-Kontext nach erfolgreichem Login. Vorgehen: Login mit IP und Device ausfuehren und im Fake-Limiter normalisierte Account-, IP- und Device-Werte im Reset pruefen.
- `UpdateProfileAsync_UpdatesOnlyProfileFieldsForUser`: prueft Selbstservice-Profilupdate ohne Sicherheitsfelder. Vorgehen: Nutzer speichern, Profilfelder mit Leerzeichen aktualisieren, getrimmte Werte und unveraenderten Passwort-Hash pruefen.
- `UpdateProfileAsync_RejectsSelfServiceCourseChanges`: prueft Verbot eigener Kurswechsel. Vorgehen: Update mit anderem Kurs senden, Fehler erwarten und unveraenderten gespeicherten Nutzer pruefen.
- `UpdateProfileAsync_RejectsInvalidProfileData`: prueft Pflichtfelder im Profil. Vorgehen: leeren Anzeigenamen senden, Fehler und unveraenderten Nutzer erwarten.

### `Features/Auth/InMemoryLoginRateLimiterTests.cs`

- `RegisterFailedAttempt_LocksOnFifthFailureWithinWindow`: prueft Schwelle fuer temporaere Sperre. Vorgehen: fuenf Fehlversuche im Zeitfenster registrieren und erst beim fuenften eine aktive Sperre erwarten.
- `CheckAndEscalateIfLimited_IncreasesTemporaryLockoutUpToOneHour`: prueft Eskalation bestehender Sperren. Vorgehen: wiederholte Checks waehrend aktiver Sperre ausfuehren und zunehmende, auf eine Stunde begrenzte Sperrdauer erwarten.
- `RegisterFailedAttempt_IgnoresAttemptsOutsideWindow`: prueft Zeitfensterbereinigung. Vorgehen: Fehlversuche erzeugen, Testzeit ueber das Fenster hinaus verschieben und sicherstellen, dass alte Versuche keine Sperre ausloesen.
- `Reset_ClearsFailureCountersAndLockout`: prueft Reset nach erfolgreichem Login. Vorgehen: Sperre erzeugen, Reset fuer denselben Kontext ausfuehren und danach keine aktive Sperre sowie leere Fehlerhistorie erwarten.

### `Features/Calendar/CalendarServiceTests.cs`

- `GetExamsAsync_ShouldReturnCurrentUsersExamsOrderedByDate`: prueft nutzerspezifische und sortierte Exam-Liste. Vorgehen: Exams fuer zwei Nutzer vorbereiten, aktuellen Nutzer abfragen und nur dessen Eintraege nach Datum erwarten.
- `AddExamAsync_ShouldRejectMissingModuleName`: prueft Validierung beim Exam-Erstellen. Vorgehen: leeren Modulnamen senden und Fehlerresultat erwarten.
- `DeleteExamAsync_ShouldRemoveOnlyCurrentUsersExam`: prueft loeschende Zugriffskontrolle. Vorgehen: Exam des aktuellen und eines anderen Nutzers speichern, beide IDs mit aktuellem Nutzer loeschen wollen und nur eigenes Exam entfernen.

### `Features/Contacts/ContactsServiceTests.cs`

- `SearchAsync_ShouldFindUsersByCourseAndProfileDetails`: prueft Kontaktsuche ueber Kurs, Telefon und Standort ohne Profilnotiz. Vorgehen: Nutzer mit Legacy-Profilnotiz und Kontaktdaten vorbereiten, Suche nach Notiz leer erwarten und Suche nach Standort/Telefon erfolgreich validieren.
- `SearchAsync_ShouldRespectRequestedLimit`: prueft Ergebnislimit. Vorgehen: mehrere passende Nutzer vorbereiten, kleines Limit anfordern und nur die begrenzte Anzahl erwarten.
- `SearchAsync_ShouldClampLimitToAtLeastOne`: prueft Limit-Untergrenze. Vorgehen: Limit kleiner eins senden und trotzdem mindestens ein Ergebnis zulassen.

### `Features/Courses/CoursesServiceTests.cs`

- `GetCoursesAsync_ShouldReturnOnlyActiveCourses`: prueft oeffentliche Kursliste. Vorgehen: aktive und inaktive Kurse bereitstellen und nur aktive Kurse erwarten.
- `CreateCourseAsync_ShouldNormalizeCodeAndCreateCourseGroup`: prueft Kursanlage. Vorgehen: Kurscode mit Kleinbuchstaben/Leerzeichen senden, normalisierten Kurs speichern und passende Kursgruppe erzeugen.
- `GetCoursesAsync_ShouldExcludeSystemCoursesFromPublicList`: prueft Ausschluss von Systemkursen. Vorgehen: `ADMIN`-Kurs und normale Kurse bereitstellen und Systemkurs aus oeffentlicher Liste ausschliessen.
- `CreateCourseAsync_ShouldRejectDuplicateCourseCodes`: prueft eindeutige Kurscodes. Vorgehen: bestehenden Kurs vorbereiten, gleichen Code erneut anlegen wollen und Fehler erwarten.

### `Features/Feed/FeedServiceTests.cs`

- `CreatePostAsync_AddsSelectedGroupMetadataToPost`: prueft Gruppenzuordnung beim Posten. Vorgehen: Gruppe und Nutzer vorbereiten, Post fuer Gruppe erstellen und gespeicherte Gruppenmetadaten im FeedPost erwarten.
- `CreatePostAsync_RejectsStudentPostsInLockedGroup`: prueft Gruppenregel `allowStudentPosts = false`. Vorgehen: Student versucht in gesperrter Gruppe zu posten, Service gibt Fehler zurueck.
- `CreatePostAsync_RejectsPostsInUnassignedPublicGroup`: prueft, dass nicht zugewiesene oeffentliche Gruppen nicht automatisch beschreibbar sind. Vorgehen: Nutzer ohne Mitgliedschaft postet in oeffentlicher Gruppe und erhaelt Fehler.
- `CreatePostAsync_RejectsMembersWhenStudentPostsDisabled`: prueft gesperrte Mitgliederposts auch fuer Gruppenmitglieder. Vorgehen: Mitgliedschaft setzen, Studentpost deaktivieren und Postversuch ablehnen.
- `CreatePostAsync_WhenApprovalIsRequired_LeavesMemberPostPending`: prueft Moderationspflicht. Vorgehen: Mitglied postet in Gruppe mit Approval-Pflicht und gespeicherter Post bleibt `Pending`.
- `CreatePostAsync_WhenApprovalIsRequired_PublishesModeratorPostImmediately`: prueft Moderator-Ausnahme. Vorgehen: Moderator postet in gleicher Konfiguration und Post wird sofort `Published`.
- `CreatePostAsync_WithTranslations_StoresGermanContentAndReturnsTranslations`: prueft mehrsprachige Feed-Beitraege. Vorgehen: deutsche, englische und franzoesische Texte senden, deutschen Hauptinhalt sowie Translations in Rueckgabe pruefen.
- `CreatePostAsync_RejectsIncompleteTranslations`: prueft Vollstaendigkeit von Uebersetzungen. Vorgehen: unvollstaendige Translation-Map senden und Validierungsfehler erwarten.
- `CreatePostAsync_WithAttachment_SavesMetadata`: prueft Attachment-Speicherung. Vorgehen: Fake-Storage und Datei verwenden, Post erstellen und Attachment-Metadaten im Ergebnis pruefen.
- `CreatePostAsync_RejectsTooManyAttachments`: prueft Attachment-Limit. Vorgehen: mehr als erlaubte Dateien uebergeben und Fehlerresultat erwarten.
- `GetFeedAsync_HidesPendingPosts`: prueft Feed-Sichtbarkeit. Vorgehen: Pending- und Published-Posts vorbereiten, Feed laden und Pending-Post ausschliessen.
- `ApprovePostAsync_AllowsModeratorAndPublishesPost`: prueft Freigabe durch Moderator. Vorgehen: Pending-Post und Moderatorrolle vorbereiten, Approve ausfuehren und `Published` erwarten.
- `GetFeedAsync_HidesPrivateUnassignedGroupPosts`: prueft private Gruppenposts fuer Nichtmitglieder. Vorgehen: private nicht zugewiesene Gruppe mit Post vorbereiten und Feed fuer Aussenstehenden ohne diesen Post erwarten.
- `GetFeedAsync_HidesPublicUnassignedGroupPosts`: prueft Interaktionsgrenze fuer oeffentliche, nicht zugewiesene Gruppen. Vorgehen: Public-Gruppe ohne Mitgliedschaft vorbereiten und Post aus dem allgemeinen Feed ausschliessen.
- `AddCommentAsync_WhenGroupAllowsComments_AppendsCommentToPost`: prueft Kommentieren bei erlaubten Kommentaren. Vorgehen: berechtigten Nutzer und Post vorbereiten, Kommentar anhaengen und Rueckgabe mit neuem Kommentar vergleichen.
- `AddCommentAsync_WhenPostDisablesComments_RejectsComment`: prueft Post-spezifische Kommentarsperre. Vorgehen: Post mit `allowComments = false` vorbereiten und Kommentarversuch ablehnen.
- `DeletePostAsync_AllowsGroupModeratorToRemoveForeignPost`: prueft Loeschrecht fuer Moderatoren. Vorgehen: Fremdpost in Gruppe vorbereiten, Moderator loescht und Repository entfernt den Post.
- `AddCommentAsync_RejectsNonMembers`: prueft Kommentarberechtigung. Vorgehen: Nichtmitglied kommentiert Gruppenpost und Service gibt Fehler zurueck.
- `ToggleReactionAsync_TogglesCurrentUserReaction`: prueft Reaktion toggeln. Vorgehen: Nutzer reagiert auf Post, Repository fuegt Reaktion hinzu beziehungsweise entfernt sie beim erneuten Toggle.
- `ToggleReactionAsync_RejectsNonMembers`: prueft Reaktionsberechtigung. Vorgehen: Nichtmitglied reagiert auf Gruppenpost und erhaelt Fehler.
- `ToggleReactionAsync_AcceptsCustomEmoji`: prueft erlaubte Emoji-Reaktionen. Vorgehen: benutzerdefiniertes Emoji senden und erfolgreiche Reaktion erwarten.
- `ToggleReactionAsync_RejectsPlainTextReaction`: prueft Emoji-Validierung. Vorgehen: Plain-Text-Reaktionswert senden und Fehler erwarten.

### `Features/Grades/GradesServiceTests.cs`

- `GetGradesAsync_ShouldCalculateWeightedAverageByEcts`: prueft gewichteten Notenschnitt. Vorgehen: Noten fuer aktuellen und anderen Nutzer vorbereiten, nur aktuelle Noten laden und ECTS-gewichteten Durchschnitt `1.67` erwarten.
- `AddGradeAsync_ShouldRejectInvalidGradeInput`: prueft Validierung fuer vier Varianten: leerer Modulname, Note `0.7`, Note `5.3` und `0` ECTS. Vorgehen: jede Variante als Theory ausfuehren und Fehlerresultat erwarten.
- `AddGradeAsync_ShouldSaveManualGrade`: prueft manuelle Notenanlage. Vorgehen: Note mit Modulcode speichern, Result und Repositoryeintrag auf Modulcode, Name und ECTS pruefen.
- `DeleteGradeAsync_ShouldRemoveOnlyCurrentUsersGrade`: prueft nutzergebundenes Loeschen. Vorgehen: eigene und fremde Note speichern, beide IDs mit aktuellem Nutzer loeschen wollen und nur eigene Note entfernen.

### `Features/Groups/GroupsServiceTests.cs`

- `GetGroupsForUserAsync_EnsuresCourseGroupFromProfile`: prueft automatische Kursgruppe aus Nutzerprofil. Vorgehen: Nutzer mit Kurs `TIF26C` abfragen und Kursgruppe im Ergebnis erwarten.
- `UpdateSettingsAsync_RejectsStudentChanges`: prueft Berechtigung fuer Gruppeneinstellungen. Vorgehen: Student versucht Kursgruppeneinstellungen zu aendern und erhaelt Permission-Fehler.
- `CreateGroupAsync_CreatesSocialGroupOwnedByUser`: prueft Campus-Gruppenerstellung durch Studenten. Vorgehen: Student erstellt Gruppe, Ergebnis ist Typ `Campus`, Owner ist Nutzer und Nutzer kann verwalten.
- `CreateGroupAsync_AllowsManagementToCreateEveryGroupType`: prueft Management-Erstellung fuer `Official`, `Course` und `Campus`. Vorgehen: Theory erstellt je Typ eine Gruppe, prueft Erfolg, Owner und bei Kursgruppen normalisierten Kurscode.
- `CreateGroupAsync_RejectsStudentForManagedGroupTypes`: prueft Studentenverbot fuer `Official` und `Course`. Vorgehen: Theory versucht beide Typen als Student und erwartet Rollenfehler.
- `CreateGroupAsync_UsesGlobalRolePermissionMatrix`: prueft die Rollenmatrix: Student darf nur `Campus`, Lecturer darf `Course` und `Campus`, Management/Admin duerfen alle drei Typen. Vorgehen: 12 Theory-Varianten ausfuehren und Erfolg oder Rollenfehler exakt gegen `canCreate` vergleichen.
- `CreateGroupAsync_AppliesInitialSettingsFromCommand`: prueft Initialwerte fuer Gruppenregeln. Vorgehen: Gruppe mit Settings aus Command erstellen und gespeicherte `allowStudentPosts`, `allowComments`, `requiresApproval`, `isDiscoverable` und `joinRule` pruefen.
- `GetGroupsForUserAsync_HidesPrivateUnassignedGroups`: prueft Sichtbarkeit privater Gruppen. Vorgehen: nicht zugewiesene private Gruppe vorbereiten und aus Nutzerliste ausschliessen.
- `GetGroupsForUserAsync_ShowsPublicUnassignedGroupsAsJoinable`: prueft Entdecken oeffentlicher Gruppen. Vorgehen: public Gruppe ohne Mitgliedschaft vorbereiten und als joinable, aber nicht assigned, im Ergebnis erwarten.
- `JoinGroupAsync_AssignsCurrentUserToPublicGroup`: prueft direkten Beitritt. Vorgehen: offene Gruppe vorbereiten, Join ausfuehren und Nutzer in Mitgliederliste erwarten.
- `CreateGroupAsync_StoresJoinRuleAndOfficialCategory`: prueft Join-Regel und offizielle Kategorie. Vorgehen: Official-Gruppe mit Kategorie und JoinRule erstellen und Werte im Ergebnis pruefen.
- `CreateGroupAsync_RejectsOfficialWithoutCategory`: prueft Pflichtkategorie fuer Official-Gruppen. Vorgehen: Official ohne Kategorie erstellen wollen und Validierungsfehler erwarten.
- `JoinGroupAsync_RequestRequired_CreatesPendingRequestWithoutMembership`: prueft Join-Request-Regel. Vorgehen: Gruppe mit Request-Regel vorbereiten, Join ausfuehren, Pending Request statt Mitgliedschaft erwarten.
- `ApproveJoinRequestAsync_AddsRequestingUserAsMember`: prueft Genehmigung von Beitrittsanfragen. Vorgehen: Pending Request vorbereiten, berechtigter Nutzer genehmigt und Antragsteller wird Mitglied.
- `InviteAndAccept_MakesInvitedUserAMember`: prueft Einladungsflow. Vorgehen: Einladung erstellen, eingeladener Nutzer nimmt an und wird Mitglied.
- `GetSettingsDetailsAsync_RejectsUnownedSocialGroup`: prueft Settings-Zugriff fuer unberechtigte Social Groups. Vorgehen: Nicht-Owner fragt Details an und bekommt Permission-Fehler.
- `AddMembersAsync_AddsExistingAccountsAsMembers`: prueft Mitgliederverwaltung. Vorgehen: existierende Nutzer-IDs hinzufuegen und Mitgliederliste der Gruppe pruefen.
- `RemoveMemberAsync_RemovesMemberButNotOwner`: prueft Entfernen normaler Mitglieder und Owner-Schutz. Vorgehen: Mitglied entfernen, danach Owner-Entfernung ablehnen.
- `LeaveGroupAsync_RemovesMemberFromGroup`: prueft normalen Gruppenaustritt. Vorgehen: Mitglied verlaesst Gruppe und wird aus Mitgliederliste entfernt.
- `LeaveGroupAsync_OwnerMustChooseNewOwnerWhenMembersRemain`: prueft Owner-Austritt mit verbleibenden Mitgliedern. Vorgehen: Owner ohne Nachfolger austreten lassen und Fehler erwarten.
- `LeaveGroupAsync_OwnerTransfersOwnershipBeforeLeaving`: prueft Owner-Transfer beim Austritt. Vorgehen: Owner gibt neue Owner-ID an, Gruppe bleibt bestehen und neuer Owner ist gesetzt.
- `LeaveGroupAsync_DeletesGroupWhenSoleOwnerLeaves`: prueft Loeschen leerer Gruppen. Vorgehen: alleiniger Owner verlaesst Gruppe und Repository loescht die Gruppe.
- `GetSettingsDetailsAsync_AllowsAssignedModerator`: prueft Settings-Zugriff fuer Moderator. Vorgehen: Moderatorrolle in Gruppe setzen und Details erfolgreich lesen.
- `GetSettingsDetailsAsync_RejectsPlainMember`: prueft Settings-Schutz fuer einfache Mitglieder. Vorgehen: Memberrolle setzen und Permission-Fehler erwarten.
- `GetGroupsForUserAsync_ExposesGroupRolesPerMember`: prueft Rollenanzeige pro Mitglied. Vorgehen: Gruppen mit Rollen vorbereiten und im Ergebnis die jeweilige Gruppenrolle erwarten.
- `GetGroupsForUserAsync_AdminHasSystemAccessWithoutGroupRole`: prueft Admin-Systemzugriff. Vorgehen: Admin ohne Gruppenrolle abfragen und Zugriff/Manage-Flags ueber Systemrolle erwarten.
- `SetMemberRoleAsync_OwnerCanAppointModerator`: prueft Rollenvergabe durch Owner. Vorgehen: Owner setzt Mitglied auf Moderator und Repositoryrolle wird aktualisiert.
- `SetMemberRoleAsync_ModeratorCannotAppointAnotherModerator`: prueft Grenzen von Moderatoren. Vorgehen: Moderator versucht, anderes Mitglied hochzustufen, und erhaelt Permission-Fehler.
- `DeleteGroupAsync_OwnerDeletesCampusGroupAndItsPosts`: prueft Gruppenloeschung samt Feedposts. Vorgehen: Owner loescht Campus-Gruppe, Gruppenrepository entfernt Gruppe und Feedrepository entfernt Gruppenposts.
- `DeleteGroupAsync_CourseGroupRequiresAdmin`: prueft Loeschschutz fuer Kursgruppen. Vorgehen: Nicht-Admin versucht Kursgruppe zu loeschen und wird abgelehnt; Admin darf loeschen.

## Frontend Unit- und Component-Tests

Frontend-Tests laufen mit Angular TestBed, Vitest und jsdom. Service-Tests verwenden HTTP-Testing-Controller oder Fakes; Komponenten-Tests rendern Standalone-Komponenten und pruefen DOM, Signals, lokale UI-Praeferenzen und Service-Aufrufe.

### App, Layout, Guards und Interceptors

- `app.spec.ts / should create the app`: prueft, dass die Root-Komponente mit TestBed erzeugt werden kann. Vorgehen: App rendern und Instanz erwarten.
- `app.spec.ts / should render title`: prueft Grundrendering der App. Vorgehen: Fixture stabilisieren und sichtbaren Anwendungstitel im DOM erwarten.
- `shell.spec.ts / should create`: prueft Shell-Komponenteninstanziierung. Vorgehen: Shell mit Router/Test-Doubles rendern und Instanz erwarten.
- `shell.spec.ts / renders legal footer links`: prueft Footer-Navigation zu Rechtstexten. Vorgehen: Shell rendern und Impressum/Datenschutz/Nutzungsordnung-Links im DOM validieren.
- `sidebar.spec.ts / should create`: prueft Sidebar-Instanziierung. Vorgehen: Komponente rendern und Instanz erwarten.
- `navbar.spec.ts / should create`: prueft Navbar-Instanziierung. Vorgehen: Navbar mit Auth-/I18n-/Theme-Fakes rendern und Instanz erwarten.
- `navbar.spec.ts / should show the current profile in the top right user menu`: prueft Profilanzeige im Benutzermenue. Vorgehen: Auth-Signal mit Profilwerten setzen, Menue oeffnen und Name/Rolle/Kurs im DOM erwarten.
- `navbar.spec.ts / should switch and persist the selected language`: prueft Sprachwechsel auf Deutsch/Englisch. Vorgehen: Einstellungsmenue bedienen, Sprache waehlen und gespeicherte Praeferenz sowie UI-Zustand kontrollieren.
- `navbar.spec.ts / should switch and persist French from the language menu`: prueft Franzoesisch im Sprachmenue. Vorgehen: Franzoesische Option waehlen und Persistenz ueber `campusconnect.language` erwarten.
- `navbar.spec.ts / should switch and persist the selected theme preference`: prueft Theme-Auswahl. Vorgehen: Appearance-Menue bedienen, Theme-Service-Aufruf und gespeicherte Praeferenz kontrollieren.
- `navbar.spec.ts / should close open menus when clicking outside them`: prueft Outside-Click-Verhalten. Vorgehen: Menues oeffnen, Dokument-Klick ausloesen und geschlossene Menues erwarten.
- `auth-guard.spec.ts / should allow logged-in users`: prueft AuthGuard fuer vorhandene Session. Vorgehen: Auth-Fake meldet eingeloggt, Guard gibt `true` zurueck.
- `auth-guard.spec.ts / should restore cookie sessions before redirecting`: prueft Cookie-Session-Restore. Vorgehen: Guard ruft Restore auf, akzeptiert danach Profil und erlaubt Navigation.
- `auth-guard.spec.ts / should redirect anonymous users to login`: prueft Redirect fuer anonyme Nutzer. Vorgehen: kein Profil nach Restore, Guard liefert UrlTree zur Login-Seite.
- `admin-guard.spec.ts / should allow admins`: prueft AdminGuard fuer Adminrolle. Vorgehen: Auth-Fake mit Adminrolle und Guard-Ergebnis `true` erwarten.
- `admin-guard.spec.ts / should redirect non-admin users to feed`: prueft AdminGuard fuer Nichtadmins. Vorgehen: Nutzerrolle Student setzen und Redirect zur Feed-Route erwarten.
- `admin-guard.spec.ts / should restore cookie sessions before checking the admin role`: prueft Restore-Reihenfolge. Vorgehen: Guard wartet auf Session-Restore und prueft erst danach Rolle.
- `auth-token-interceptor.spec.ts / should attach bearer tokens when available`: prueft Authorization-Header. Vorgehen: Auth-Fake liefert Token, HTTP-Request absetzen und `Bearer`-Header erwarten.
- `auth-token-interceptor.spec.ts / should leave requests unchanged when no token exists`: prueft tokenlosen Pfad. Vorgehen: Auth-Fake ohne Token, Request absenden und fehlenden Authorization-Header erwarten.
- `error-handler-interceptor.spec.ts / should log out the user on unauthorized API responses`: prueft globales `401`-Handling. Vorgehen: API-Request mit `401` flushen und Logout-Aufruf im Auth-Fake erwarten.

### Core Services und I18n

- `admin.spec.ts / should create courses through the admin endpoint`: prueft `POST /api/admin/courses`. Vorgehen: Service aufrufen, Methode/URL/Body im HTTP-Testrequest vergleichen.
- `admin.spec.ts / should update user roles with a patch request`: prueft Rollen-Patch. Vorgehen: Service-Methode aufrufen und PATCH auf `/api/admin/users/{id}/role` mit Rolle erwarten.
- `admin.spec.ts / should update user status with a patch request`: prueft Status-Patch. Vorgehen: Service-Methode aufrufen und PATCH auf `/api/admin/users/{id}/status` mit Aktivstatus erwarten.
- `admin.spec.ts / should reset user passwords with a patch request`: prueft Passwort-Reset-Payload. Vorgehen: Service-Methode aufrufen und PATCH auf `/api/admin/users/{id}/password` mit `initialPassword` erwarten.
- `admin.spec.ts / should delete users through the admin endpoint`: prueft Nutzerloeschung. Vorgehen: Service-Methode aufrufen und DELETE auf `/api/admin/users/{id}` erwarten.
- `auth.spec.ts / should be created`: prueft Auth-Service-Erzeugung. Vorgehen: Service aus TestBed beziehen und Instanz erwarten.
- `auth.spec.ts / should store the full profile returned by login`: prueft Login-State. Vorgehen: Login-HTTP-Antwort mit Token/Profil flushen und gespeichertes Profil sowie Token-Signal erwarten.
- `auth.spec.ts / should restore an authenticated cookie session from the profile endpoint`: prueft Session-Restore via `/api/auth/me`. Vorgehen: Restore aufrufen, Profilantwort flushen und authentifizierten Zustand erwarten.
- `auth.spec.ts / should clear the session after 15 minutes without activity`: prueft Inaktivitaets-Timeout. Vorgehen: Fake-Timer vorlaufen lassen und geloeschtes Profil/Token erwarten.
- `auth.spec.ts / should reset the inactivity timer when the user is active`: prueft Aktivitaetsverlangerung. Vorgehen: Timer starten, Aktivitaet triggern, Zeit fortschreiben und erst nach erneutem Timeout Logout erwarten.
- `auth.spec.ts / should update the cached profile after saving changes`: prueft Profil-Update im Auth-Service. Vorgehen: PUT ausfuehren, neue Profilantwort flushen und Cache-Signal vergleichen.
- `calendar.spec.ts / should be created`: prueft Calendar-Service-Erzeugung. Vorgehen: Service aus TestBed beziehen.
- `calendar.spec.ts / should create exam entries through the API`: prueft Exam-Anlage. Vorgehen: Service-Aufruf und POST auf `/api/calendar` mit Body validieren.
- `calendar.spec.ts / should delete exam entries through the API`: prueft Exam-Loeschung. Vorgehen: Service-Aufruf und DELETE auf `/api/calendar/{id}` validieren.
- `contacts.spec.ts / should search contacts with trimmed query text`: prueft Suchquery-Normalisierung. Vorgehen: Service mit Leerzeichen im Suchtext aufrufen und GET-URL mit getrimmtem Query erwarten.
- `contacts.spec.ts / should include an optional result limit`: prueft Limit-Parameter. Vorgehen: Service mit Limit aufrufen und Querystring inklusive Limit erwarten.
- `courses.spec.ts / should load active courses from the public courses endpoint`: prueft Kursservice. Vorgehen: GET auf `/api/courses` erwarten und Antwort als Kursliste zurueckgeben.
- `feed.spec.ts / should be created`: prueft Feed-Service-Erzeugung. Vorgehen: Service aus TestBed beziehen.
- `feed.spec.ts / should send the selected group when creating a post`: prueft JSON-Postanlage. Vorgehen: `createPost` mit `groupId` aufrufen und POST-Body pruefen.
- `feed.spec.ts / should send multipart form data for translated posts with attachments`: prueft Multipart-Erstellung. Vorgehen: Translations und Datei uebergeben, Request als `FormData` erwarten und Felder/Dateien vergleichen.
- `feed.spec.ts / should load and approve pending posts`: prueft Moderationsendpunkte. Vorgehen: GET fuer Pending Posts und POST fuer Approve ausfuehren und URLs kontrollieren.
- `grades.spec.ts / should load the current grade summary`: prueft Notenladen. Vorgehen: GET `/api/grades` erwarten und Summary mappen.
- `grades.spec.ts / should add a grade through the API`: prueft Notenanlage. Vorgehen: POST `/api/grades` mit Modul-/Noten-/ECTS-Daten erwarten.
- `grades.spec.ts / should delete a grade through the API`: prueft Notenloeschung. Vorgehen: DELETE `/api/grades/{id}` erwarten.
- `groups.spec.ts / should be created`: prueft Groups-Service-Erzeugung. Vorgehen: Service aus TestBed beziehen.
- `groups.spec.ts / should update settings for a group`: prueft Settings-Update. Vorgehen: PUT auf `/api/groups/{id}/settings` mit Settings-Body erwarten.
- `groups.spec.ts / should create a group`: prueft Gruppenanlage. Vorgehen: POST `/api/groups` mit Erstellungsdaten erwarten.
- `groups.spec.ts / should load settings details for a group`: prueft Settings-Details. Vorgehen: GET `/api/groups/{id}/settings` erwarten.
- `groups.spec.ts / should delete a group`: prueft Gruppenloeschung. Vorgehen: DELETE `/api/groups/{id}` erwarten.
- `groups.spec.ts / should search candidates for a group`: prueft Kandidatensuche. Vorgehen: GET auf Candidates-Endpunkt mit Query erwarten.
- `groups.spec.ts / should add members to a group`: prueft Mitgliederanlage. Vorgehen: POST `/members` mit User-ID-Liste erwarten.
- `groups.spec.ts / should add a whole course to a group`: prueft Kurszuweisung. Vorgehen: POST `/members/course` mit Kurscode erwarten.
- `groups.spec.ts / should remove a member from a group`: prueft Mitglied entfernen. Vorgehen: DELETE `/members/{userId}` erwarten.
- `groups.spec.ts / should set a member role`: prueft Gruppenrollenwechsel. Vorgehen: PUT `/members/{userId}/role` mit Rolle erwarten.
- `groups.spec.ts / should leave a group`: prueft Gruppenaustritt. Vorgehen: POST `/api/groups/{id}/leave` mit optionalem neuen Owner erwarten.
- `mensa.spec.ts / should be created`: prueft Mensa-Service-Erzeugung. Vorgehen: Service aus TestBed beziehen.
- `mensa.spec.ts / should load the week menu from the backend API`: prueft Mensa-API-Aufruf. Vorgehen: GET `/api/mensa` erwarten und Wochenmenue mappen.
- `theme.spec.ts / defaults to system preference and applies the current system theme`: prueft System-Default. Vorgehen: `matchMedia` faken, Service initialisieren und DOM-Theme/Color-Scheme pruefen.
- `theme.spec.ts / follows system changes while preference is system`: prueft Reaktion auf System-Themewechsel. Vorgehen: MediaQuery-Change ausloesen und aktualisiertes Dataset erwarten.
- `theme.spec.ts / loads a stored theme preference`: prueft gespeicherte Theme-Praeferenz. Vorgehen: localStorage setzen, Service starten und angewendetes Theme erwarten.
- `theme.spec.ts / stores and applies an explicit theme preference`: prueft explizite Theme-Auswahl. Vorgehen: Theme setzen und localStorage plus DOM-Attribute kontrollieren.
- `theme.spec.ts / ignores invalid theme preferences`: prueft robuste Praeferenzvalidierung. Vorgehen: ungueltigen localStorage-Wert setzen und Fallback auf System erwarten.
- `timetable.spec.ts / should request the full supported timetable window by default`: prueft Default-Zeitfenster. Vorgehen: Service aufrufen und GET mit Default-Days erwarten.
- `timetable.spec.ts / should allow the backend to resolve the profile course when none is provided`: prueft optionalen Kursparameter. Vorgehen: Service ohne Kurs aufrufen und URL ohne Course-Query erwarten.
- `timetable.spec.ts / should request an explicit timetable range start`: prueft `from`-Query. Vorgehen: Startdatum uebergeben und Querystring vergleichen.
- `timetable.spec.ts / should build course options from backend courses and stored history only`: prueft Kursoptionsaufbau. Vorgehen: Backendkurse und gespeicherte Historie faken, normalisierte und deduplizierte Optionen erwarten.
- `i18n.spec.ts / defaults to German when no language is stored`: prueft Sprachdefault. Vorgehen: localStorage leeren, Service starten und `de` erwarten.
- `i18n.spec.ts / loads a stored language preference`: prueft gespeichertes Englisch. Vorgehen: `campusconnect.language` setzen und Sprache laden.
- `i18n.spec.ts / loads a stored French language preference`: prueft gespeichertes Franzoesisch. Vorgehen: `fr` speichern und Service-Sprache erwarten.
- `i18n.spec.ts / normalizes a stored language preference`: prueft Normalisierung gespeicherter Werte. Vorgehen: Wert mit abweichender Schreibweise speichern und normalisierte Sprache erwarten.
- `i18n.spec.ts / ignores invalid language values`: prueft Fallback bei ungueltiger Sprache. Vorgehen: invaliden Wert setzen und Deutsch erwarten.
- `i18n.spec.ts / removes invalid stored language values`: prueft Aufraeumen ungueltiger Praeferenzen. Vorgehen: invaliden localStorage-Wert setzen, Service starten und entfernten Eintrag erwarten.
- `i18n.spec.ts / updates the document language when language changes`: prueft `document.documentElement.lang`. Vorgehen: Sprache wechseln und DOM-Sprache kontrollieren.
- `i18n.spec.ts / normalizes and persists French when language changes`: prueft Persistenz beim Setzen. Vorgehen: Franzoesisch setzen und gespeicherten normalisierten Wert erwarten.
- `i18n.spec.ts / keeps translation keys and interpolation parameters aligned across languages`: prueft Vollstaendigkeit der Uebersetzungen. Vorgehen: Translation-Key-Sets und Platzhalter zwischen Deutsch, Englisch und Franzoesisch vergleichen.
- `i18n.spec.ts / interpolates translation parameters`: prueft Parameterersetzung. Vorgehen: Text mit Platzhaltern uebersetzen und eingesetzte Werte erwarten.
- `i18n.spec.ts / localizes known backend errors`: prueft bekannte Backend-Fehler. Vorgehen: Fehlertext an `readError` geben und lokalisierte Meldung erwarten.
- `i18n.spec.ts / uses the localized fallback for unknown backend errors`: prueft Fallback-Handling. Vorgehen: unbekannten Fehler mit Fallback-Key lesen und lokalisierte Fallback-Meldung erwarten.

### Feature-Komponenten

- `admin-page.spec.ts / should create`: prueft AdminPage-Instanziierung. Vorgehen: Komponente mit Admin-Service-Fakes rendern.
- `admin-page.spec.ts / persists admin tabs and filters`: prueft Speichern von Tab- und Filterpraeferenzen. Vorgehen: aktive Admin-Tabs/Filter setzen und localStorage-Werte kontrollieren.
- `admin-page.spec.ts / restores admin tabs and filters`: prueft Wiederherstellung gespeicherter Admin-Praeferenzen. Vorgehen: localStorage vorbereiten, Komponente initialisieren und aktive UI-Zustaende vergleichen.
- `admin-page.spec.ts / generates a secure initial password`: prueft Passwortgenerator. Vorgehen: Generator ausfuehren und Laenge sowie Zeichenklassen validieren.
- `admin-page.spec.ts / resets a selected user password`: prueft Reset-Aktion im Admin-Editor. Vorgehen: Nutzer in den Bearbeitungsdialog laden, Initialpasswort setzen und Service-Aufruf mit Nutzer-ID und Passwort erwarten.
- `login-page.spec.ts / should create`: prueft LoginPage-Instanziierung. Vorgehen: Komponente mit Auth-Fake rendern.
- `login-page.spec.ts / should render only the login form`: prueft, dass keine Registrierungs-UI mehr angeboten wird. Vorgehen: DOM rendern und nur Login-Formular erwarten.
- `login-page.spec.ts / localizes known backend login errors`: prueft Fehlerlokalisierung im Login. Vorgehen: Auth-Login mit bekannter Fehlermeldung scheitern lassen und lokalisierte UI-Meldung erwarten.
- `calendar-page.spec.ts / should create`: prueft CalendarPage-Instanziierung. Vorgehen: Komponente rendern und Instanz erwarten.
- `contact-book-page.spec.ts / should show the search entry point and favorites section without a full contact list`: prueft Kontaktbuch-Startzustand. Vorgehen: Seite rendern und Suchkarte/Favoriten statt Vollkontaktliste erwarten.
- `contact-book-page.spec.ts / should open the contact search modal from the search card`: prueft Suchmodal-Oeffnung. Vorgehen: Suchkarte klicken und Dialog im DOM erwarten.
- `contact-book-page.spec.ts / should add and remove a favorite from the search results`: prueft Favoriteninteraktion und sichtbare Kontaktdaten. Vorgehen: Suchergebnis mit Telefon/Standort im Modal favorisieren, Favorit im Kontaktbuch erwarten, danach entfernen und Verschwinden pruefen.
- `contact-book-page.spec.ts / restores saved favorite contacts`: prueft Persistenz der Favoriten. Vorgehen: localStorage mit Favorit vorbereiten, Seite initialisieren und Favoritenkarte erwarten.
- `contact-search-modal.spec.ts / should show a minimum length hint before searching`: prueft Mindestlaengen-Hinweis. Vorgehen: Modal ohne ausreichende Eingabe rendern und Hinweis statt Request erwarten.
- `contact-search-modal.spec.ts / should wait for at least 3 characters before sending a debounced search request`: prueft Debounce und Mindestlaenge. Vorgehen: kurze und danach gueltige Eingabe setzen, Timer fortschreiben und erst dann Service-Suche erwarten.
- `contact-search-modal.spec.ts / should render compact search results`: prueft Ergebnisdarstellung. Vorgehen: Suchservice liefert Kontakte, Modal zeigt Name, Kurs, Studiengang, Telefon, Standort und E-Mail.
- `feed-page.spec.ts / should create`: prueft FeedPage-Instanziierung. Vorgehen: Komponente mit Auth-, Feed-, Groups- und Timetable-Fakes rendern.
- `feed-page.spec.ts / persists the selected posting group`: prueft ausgewaehlte Posting-Gruppe. Vorgehen: interne Auswahl setzen und localStorage-Key `campusconnect.feed.selectedGroupId` vergleichen.
- `feed-page.spec.ts / starts with a compact composer and expands from the prompt`: prueft Composer-Initialzustand. Vorgehen: Seite rendern, geschlossenes Panel erwarten, Prompt klicken und offenes Panel erwarten.
- `feed-page.spec.ts / updates composer switch states from the settings panel`: prueft Composer-Schalter. Vorgehen: Composer/Settings oeffnen, Kommentare deaktivieren, Uebersetzungen aktivieren und Signalwerte sowie deutsches Textfeld pruefen.
- `feed-page.spec.ts / shows, removes, and validates selected attachments`: prueft Dateiauswahl. Vorgehen: PDF hinzufuegen und anzeigen, entfernen, ungueltige `.exe` ablehnen und Limit von fuenf Dateien pruefen.
- `feed-page.spec.ts / renders DHBW quick access links as external redirects`: prueft Schnellzugriffe. Vorgehen: Linktexte, Untertitel, Ziel-URLs, `_blank` und `noopener noreferrer` vergleichen.
- `feed-page.spec.ts / opens image attachments in a preview with a download action`: prueft Bildvorschau. Vorgehen: Post mit Image-Attachment setzen, Preview oeffnen, Dateiname/Download-Link pruefen und per Escape-Helfer schliessen.
- `feed-page.spec.ts / opens the comment composer from the compact comment button`: prueft Kommentar-Composer. Vorgehen: Post setzen, Kommentieren-Button klicken und sichtbares Composer-Formular erwarten.
- `feed-page.spec.ts / submits a picked emoji reaction`: prueft Reaction-Service-Aufruf. Vorgehen: Post setzen, Emoji waehlen und `toggleReaction(postId, { emoji })` erwarten.
- `feed-page.spec.ts / does not delete posts when confirmation is cancelled`: prueft Abbruch bei Loeschbestaetigung. Vorgehen: `confirm` gibt `false` zurueck, `deletePost` darf nicht aufgerufen werden.
- `feed-page.spec.ts / deletes posts after confirmation`: prueft bestaetigtes Loeschen. Vorgehen: `confirm` gibt `true` zurueck, Dialogtext und `deletePost('post-1')` erwarten.
- `feed-page.spec.ts / loads and sorts the current day schedule`: prueft Tagesplan auf Feed-Seite. Vorgehen: Fake-Zeit setzen, unsortierte Termine liefern und sortierte Schedule-Events fuer Kurs `TIF25A` erwarten.
- `feed-page.spec.ts / shows a course selection action when no schedule course is available`: prueft leeren Kurszustand. Vorgehen: Nutzerprofil und gespeicherten Kurs entfernen, kein Timetable-Request und Kursauswahl-Meldung erwarten.
- `feed-page.spec.ts / clears schedule events and shows an error when schedule loading fails`: prueft Fehlerzustand des Tagesplans. Vorgehen: Timetable-Service wirft Fehler, Events werden geleert und Fehlermeldung gesetzt.
- `feed-page.spec.ts / clears stale feed errors on a successful reload and prevents duplicate posts`: prueft Fehlerreset und Doppelpost-Schutz. Vorgehen: alten Fehler setzen, Feed neu laden, Fehler leeren; waehrend `_isPosting` kein weiterer `createPost`-Aufruf.
- `feed-page.spec.ts / submits translated posts with selected files`: prueft mehrsprachigen Multipart-Post aus der UI. Vorgehen: Uebersetzungen und Datei setzen, Post absenden und Feed-Service-Command inklusive Translations/Attachments sowie resetten Composerzustand pruefen.
- `feed-page.spec.ts / keeps the composer open and shows translated backend validation errors`: prueft Backend-Validierungsfehler. Vorgehen: `createPost` mit 400 und bekanntem Fehler scheitern lassen, Composer bleibt offen und lokalisierte Meldung wird angezeigt.
- `feed-page.spec.ts / uses the active language for translated post content`: prueft lokalisierte Post-Anzeige. Vorgehen: Post mit drei Uebersetzungen und Sprache `en` setzen, lokalisierter Inhalt ist Englisch.
- `grades-page.spec.ts / should create`: prueft GradesPage-Instanziierung. Vorgehen: Komponente mit Grades-Service-Fake rendern.
- `grades-page.spec.ts / should load grades from the backend service`: prueft Laden der Noten. Vorgehen: Service liefert Summary und Komponentensignal/DOM uebernimmt Werte.
- `grades-page.spec.ts / should add manual grades through the backend service and calculate weighted averages`: prueft manuelle Eingabe. Vorgehen: Formularwerte setzen, Add-Service aufrufen lassen und aktualisierten gewichteten Durchschnitt erwarten.
- `grades-page.spec.ts / should preview additional grades without saving them`: prueft Simulationsfunktion. Vorgehen: Preview-Note setzen und Durchschnitt berechnen, ohne Add-Service aufzurufen.
- `grades-page.spec.ts / should delete grades through the backend service`: prueft Loeschaktion. Vorgehen: vorhandene Note loeschen und `deleteGrade` plus Reload/State-Aktualisierung erwarten.
- `grades-page.spec.ts / should reject empty manual module names`: prueft Frontend-Validierung. Vorgehen: leeren Modulnamen setzen, Submit ausfuehren und keinen API-Aufruf erwarten.
- `group-detail-page.spec.ts / shows the selected group posts`: prueft Gruppenfeed-Anzeige. Vorgehen: Feed-Fake mit Gruppenposts setzen und passende Inhalte im DOM erwarten.
- `group-detail-page.spec.ts / creates posts for the selected group from the form submit`: prueft Gruppenpost-Erstellung. Vorgehen: Formularinhalt setzen, Submit ausfuehren und Feed-Service mit aktueller Gruppen-ID erwarten.
- `group-detail-page.spec.ts / creates comments from the comment form submit`: prueft Kommentare im Gruppendetail. Vorgehen: Kommentartext setzen, Submit ausfuehren und `createComment` mit Post-ID erwarten.
- `group-detail-page.spec.ts / submits a picked emoji reaction`: prueft Reaktionen im Gruppendetail. Vorgehen: Emoji waehlen und `toggleReaction` mit Post-ID/Emoji erwarten.
- `group-detail-page.spec.ts / does not delete group posts when confirmation is cancelled`: prueft Loeschabbruch. Vorgehen: `confirm = false`, kein `deletePost`-Aufruf.
- `group-detail-page.spec.ts / deletes group posts after confirmation`: prueft bestaetigtes Post-Loeschen. Vorgehen: `confirm = true`, `deletePost` wird mit Post-ID aufgerufen.
- `group-settings-page.spec.ts / should create`: prueft SettingsPage-Instanziierung. Vorgehen: Komponente mit Gruppen-/Feed-Fakes rendern.
- `group-settings-page.spec.ts / lists current members with their group role`: prueft Mitgliederliste. Vorgehen: Settings-Details mit Rollen liefern und Rollen im DOM erwarten.
- `group-settings-page.spec.ts / allows role editing for non-owner members only`: prueft Rollenbearbeitung. Vorgehen: Owner und Member rendern, Bearbeitung nur fuer Nicht-Owner erlauben.
- `group-settings-page.spec.ts / shows posts waiting for approval`: prueft Pending-Posts-Ansicht. Vorgehen: Pending-Posts liefern und Inhalte im Moderationsbereich erwarten.
- `group-settings-page.spec.ts / does not reject pending posts when confirmation is cancelled`: prueft Ablehnungsabbruch. Vorgehen: `confirm = false`, Pending-Post bleibt und kein Loesch-/Reject-Aufruf.
- `group-settings-page.spec.ts / rejects pending posts after confirmation`: prueft bestaetigte Ablehnung. Vorgehen: `confirm = true`, Reject/Delete-Aufruf ausfuehren und Pending-Liste aktualisieren.
- `group-settings-page.spec.ts / requires a second step before group deletion`: prueft zweistufige Gruppenloeschung. Vorgehen: ersten Loeschklick als Bestaetigungszustand pruefen und erst zweiten Schritt als API-Loeschung zulassen.
- `groups-page.spec.ts / should create`: prueft GroupsPage-Instanziierung. Vorgehen: Komponente mit Groups-/Auth-Fakes rendern.
- `groups-page.spec.ts / persists the selected tab and policy filter`: prueft Speicherung von Tab und Join-Policy-Filter. Vorgehen: Werte setzen und localStorage vergleichen.
- `groups-page.spec.ts / restores the selected tab and policy filter`: prueft Wiederherstellung gespeicherter Gruppenansicht. Vorgehen: localStorage vorbereiten und initiale Signalwerte kontrollieren.
- `groups-page.spec.ts / filters visible groups by search text`: prueft Suchfilter. Vorgehen: Gruppenliste setzen, Suchtext eingeben und sichtbare Gruppen reduzieren.
- `groups-page.spec.ts / shows joinable public groups in explore tab`: prueft Explore-Tab. Vorgehen: joinable Gruppe vorbereiten, Tab wechseln und Beitrittsaktion erwarten.
- `groups-page.spec.ts / shows only campus group creation to students`: prueft Erstelloptionen fuer Studenten. Vorgehen: Studentrolle setzen und nur Campus-Gruppentyp anbieten.
- `groups-page.spec.ts / shows course creation to lecturers but not official creation`: prueft Erstelloptionen fuer Lecturers. Vorgehen: Lecturerrolle setzen und Course/Campus, aber nicht Official erwarten.
- `groups-page.spec.ts / does not submit a group type forbidden for the current role`: prueft Submit-Schutz. Vorgehen: verbotenen Typ manipulativ setzen und keinen `createGroup`-Aufruf erwarten.
- `groups-page.spec.ts / sends an allowed selected group type when creating a group`: prueft erlaubte Gruppenerstellung. Vorgehen: gueltigen Typ auswaehlen, Formular senden und Command an Groups-Service vergleichen.
- `groups-page.spec.ts / keeps request-based groups in the explore tab`: prueft Sichtbarkeit von Gruppen mit Beitrittsanfrage. Vorgehen: Request-Gruppe vorbereiten und im Explore-Tab belassen.
- `legal-page.spec.ts / renders the selected legal page with the project notice`: prueft Impressum/Standard-Rechtsseite. Vorgehen: Route-Daten setzen und Projektplatzhalter-Hinweis rendern.
- `legal-page.spec.ts / renders the privacy policy for the local pilot operation`: prueft Datenschutzseite. Vorgehen: Datenschutz-Route rendern und lokale Pilotbetriebsinhalte erwarten.
- `legal-page.spec.ts / renders the usage rules for group communication without future chat wording`: prueft Nutzungsordnung. Vorgehen: Nutzungsordnungs-Route rendern und sicherstellen, dass keine Chat-Zukunftsversprechen erscheinen.
- `legal-page.spec.ts / links the legal brand back to the authenticated home area`: prueft Navigationsziel des Brands. Vorgehen: Brand-Link auslesen und Ziel zur authentifizierten Home-/Feed-Area erwarten.
- `mensa-page.spec.ts / should create`: prueft MensaPage-Instanziierung. Vorgehen: Komponente mit Mensa-Service-Fake rendern.
- `mensa-page.spec.ts / should ignore day selections outside the loaded menu`: prueft robuste Tagesauswahl. Vorgehen: nicht vorhandenes Datum waehlen und selektierten Tag unveraendert lassen.
- `mensa-page.spec.ts / persists the selected menu day by date`: prueft Tagespraeferenz. Vorgehen: geladenen Tag waehlen und Datum im localStorage speichern.
- `mensa-page.spec.ts / should derive readable category markers`: prueft Kategorien aus Gerichtsnamen. Vorgehen: Menuezeilen mit Markern auswerten und lesbare Kategorien erwarten.
- `mensa-page.spec.ts / should fall back to the dish name when no pre-split name lines exist`: prueft Fallback fuer Gerichtsanzeige. Vorgehen: Gericht ohne vorgeteilte Namenszeilen rendern und Namen als Anzeige verwenden.
- `profile-page.spec.ts / should load the current user profile`: prueft Profilseite ohne Profilnotiz-Feld. Vorgehen: Auth-Service liefert aktuelles Profil und Formular/Anzeige werden befuellt; Profilnotiz-Text darf nicht erscheinen.
- `profile-page.spec.ts / shows the assigned course as read-only profile data`: prueft Kurs-Readonly-Regel. Vorgehen: Profil rendern und Kurs als nicht editierbare Information statt Eingabe erwarten.
- `timetable-page.spec.ts / should create`: prueft TimetablePage-Instanziierung. Vorgehen: Komponente mit Timetable-Service-Fake rendern.
- `timetable-page.spec.ts / should restore and persist the selected timetable view`: prueft Ansichtspraeferenz. Vorgehen: gespeicherte Ansicht laden, Ansicht wechseln und localStorage aktualisieren.
- `timetable-page.spec.ts / should size week events proportionally to their duration`: prueft Wochenlayout. Vorgehen: Termine mit unterschiedlicher Dauer berechnen lassen und proportionale Hoehen/Positionen erwarten.
- `timetable-page.spec.ts / should keep past days visible in the selected week`: prueft sichtbare Woche inklusive vergangener Tage. Vorgehen: Datum in Woche setzen und alle Wochentage im Raster behalten.
- `timetable-page.spec.ts / should load the visible past week when navigating backwards`: prueft Ruecknavigation. Vorgehen: Woche zurueck navigieren und Timetable-Service mit sichtbarem Vergangenheits-Start aufrufen.
- `timetable-page.spec.ts / should keep day view scoped to the selected date`: prueft Tagesansicht. Vorgehen: selektiertes Datum setzen und nur Termine dieses Datums anzeigen.
- `timetable-page.spec.ts / should mark short events as compact without losing their details`: prueft Darstellung kurzer Termine. Vorgehen: kurzer Termin wird als kompakt markiert, Titel/Ort/Zeit bleiben verfuegbar.
- `timetable-page.spec.ts / should extend the timeline when events are outside the regular day`: prueft dynamische Zeitachse. Vorgehen: fruehe/spaete Termine liefern und Timeline-Grenzen erweitern.
- `timetable-page.spec.ts / should position the current time marker on the visible day`: prueft Jetzt-Markierung. Vorgehen: Fake-Zeit innerhalb sichtbarem Tag setzen und Markerposition im Zeitraster erwarten.
- `timetable-page.spec.ts / should hide the current time marker outside the visible timeline`: prueft Marker-Ausblendung. Vorgehen: Fake-Zeit ausserhalb sichtbarer Timeline setzen und keinen Marker anzeigen.

## Playwright-Smoke-Tests

Die E2E-Smoke-Tests starten ueber `npm run e2e` mit gebautem Frontend, API und isolierter E2E-SQLite-Datenbank. Jeder Test prueft zusaetzlich, dass die Seite keinen horizontalen Overflow erzeugt.

- `public legal placeholder pages are reachable without login`: prueft oeffentliche Rechtstexte ohne Login. Vorgehen: `/legal/impressum`, `/legal/datenschutz` und `/legal/nutzungsordnung` direkt aufrufen, jeweilige H1, Repository-Platzhalterhinweis und fehlenden horizontalen Overflow erwarten.
- `demo student can sign in, create a feed post, navigate core features, and sign out`: prueft zentralen Student-Smoke-Flow. Vorgehen: Demo-Student einloggen, Feed-Post mit deaktivierten Kommentaren erstellen, Mensa, Stundenplan mit Liste/Woche/Tag, Noten, Gruppen inklusive Erstellen-Dialog/Entdecken, Kontakte-Suchdialog aufrufen und danach ueber Benutzermenue ausloggen.
- `demo admin can open the admin area`: prueft Admin-Smoke-Flow. Vorgehen: Demo-Admin einloggen, Adminbereich oeffnen, Benutzerverwaltung und Dialog fuer neuen Nutzer pruefen, Dialog schliessen, Kursverwaltung oeffnen und horizontalen Overflow ausschliessen.
