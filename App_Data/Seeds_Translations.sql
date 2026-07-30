-- =============================================================================
-- EidUbahle ERP – Default Translation Seeds (English + core keys)
-- Run after Schema_Phase1.sql
-- =============================================================================

USE EidUbahleDB;
GO

-- Helper: clean insert using the upsert SP
DECLARE @Null UNIQUEIDENTIFIER = NULL;

-- ─── Login / Auth ─────────────────────────────────────────────────────────────
EXEC sp_Translation_Upsert NEWID(),@Null,'en','login.title','Sign In to EidUbahle ERP','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','login.subtitle','Manage your business with confidence','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','login.username','Username or Email','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','login.password','Password','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','login.remember','Remember me','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','login.forgot','Forgot Password?','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','login.signin','Sign In','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','login.signing_in','Signing in…','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','login.2fa_title','Two-Factor Authentication','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','login.2fa_label','Enter 6-digit code from your authenticator app','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','login.2fa_verify','Verify','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','login.offline_mode','You are working offline. Data is saved locally.','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','login.error.empty','Username and password are required.','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','login.error.invalid','Invalid username or password.','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','login.error.locked','Account is locked. Try again later.','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','login.error.inactive','Account is inactive. Contact your administrator.','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','login.error.2fa','Invalid 2FA code.','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','logout.success','You have been signed out.','Auth',0;

-- Somali translations (auth)
EXEC sp_Translation_Upsert NEWID(),@Null,'so','login.title','Gal EidUbahle ERP','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'so','login.username','Magaca isticmaalaha ama Iimeelka','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'so','login.password','Furaha sirta ah','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'so','login.signin','Gal','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'so','login.offline_mode','Waxaad shaqeynaysaa si aan toos ah. Xogta waxaa lagu kaydiyaa meel adag.','Auth',0;

-- Arabic translations (auth)
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','login.title','تسجيل الدخول إلى EidUbahle ERP','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','login.username','اسم المستخدم أو البريد الإلكتروني','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','login.password','كلمة المرور','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','login.signin','دخول','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','login.offline_mode','أنت تعمل دون اتصال بالإنترنت. يتم حفظ البيانات محليًا.','Auth',0;

-- French
EXEC sp_Translation_Upsert NEWID(),@Null,'fr','login.title','Connexion à EidUbahle ERP','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'fr','login.username','Nom d''utilisateur ou e-mail','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'fr','login.password','Mot de passe','Auth',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'fr','login.signin','Se connecter','Auth',0;

-- ─── Navigation ───────────────────────────────────────────────────────────────
EXEC sp_Translation_Upsert NEWID(),@Null,'en','nav.dashboard','Dashboard','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','nav.accounting','Accounting','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','nav.inventory','Inventory','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','nav.sales','Sales','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','nav.purchases','Purchases','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','nav.banking','Banking','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','nav.crm','CRM','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','nav.hr','HR & Payroll','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','nav.reports','Reports','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','nav.admin','Admin','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','nav.settings','Settings','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','nav.profile','My Profile','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','nav.logout','Sign Out','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','nav.sync_status','Sync Status','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','nav.help','Help & Tours','Navigation',0;
-- Somali nav
EXEC sp_Translation_Upsert NEWID(),@Null,'so','nav.dashboard','Shaxda','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'so','nav.accounting','Xisaabaadka','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'so','nav.inventory','Kaydka','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'so','nav.sales','Iibka','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'so','nav.purchases','Iibsashada','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'so','nav.reports','Warbixinnada','Navigation',0;
-- Arabic nav
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','nav.dashboard','لوحة التحكم','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','nav.accounting','المحاسبة','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','nav.inventory','المخزون','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','nav.sales','المبيعات','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','nav.purchases','المشتريات','Navigation',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','nav.reports','التقارير','Navigation',0;

-- ─── Common UI ────────────────────────────────────────────────────────────────
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.save','Save','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.cancel','Cancel','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.delete','Delete','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.edit','Edit','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.add','Add','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.search','Search','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.filter','Filter','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.export','Export','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.import','Import','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.print','Print','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.approve','Approve','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.reject','Reject','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.post','Post','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.draft','Draft','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.loading','Loading…','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.saving','Saving…','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.success','Operation completed successfully.','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.error','An error occurred. Please try again.','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.confirm_delete','Are you sure you want to delete this record?','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.no_data','No records found.','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.rows_per_page','Rows per page','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.page','Page','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.of','of','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.total','Total','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.yes','Yes','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.no','No','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.back','Back','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.next','Next','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.close','Close','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.actions','Actions','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.status','Status','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.date','Date','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.amount','Amount','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.description','Description','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.name','Name','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.code','Code','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.online','Online','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.offline','Offline','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.sync_now','Sync Now','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.last_synced','Last synced {0} ago','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','common.pending_sync','{0} changes pending sync','Common',0;
-- Arabic common
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','common.save','حفظ','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','common.cancel','إلغاء','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','common.delete','حذف','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','common.search','بحث','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','common.loading','جار التحميل…','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','common.success','تمت العملية بنجاح.','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','common.error','حدث خطأ. يرجى المحاولة مرة أخرى.','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','common.offline','غير متصل','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'ar','common.sync_now','مزامنة الآن','Common',0;
-- Somali common
EXEC sp_Translation_Upsert NEWID(),@Null,'so','common.save','Kaydi','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'so','common.cancel','Jooji','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'so','common.search','Raadi','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'so','common.loading','Rarida…','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'so','common.offline','Aan xiriir lahayn','Common',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'so','common.sync_now','Isku dheeli hada','Common',0;

-- ─── Admin / Translations page ────────────────────────────────────────────────
EXEC sp_Translation_Upsert NEWID(),@Null,'en','admin.translations.title','Translation Management','Admin',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','admin.translations.add_language','Add Language','Admin',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','admin.translations.export_json','Export JSON','Admin',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','admin.translations.import_json','Import JSON','Admin',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','admin.translations.import_excel','Import Excel','Admin',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','admin.translations.key','Translation Key','Admin',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','admin.translations.module','Module','Admin',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','admin.translations.saved','Translation saved.','Admin',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','admin.translations.direction_note','RTL languages (Arabic, Hebrew) automatically flip the layout.','Admin',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','admin.translations.import_success','{0} translations imported successfully.','Admin',0;

-- ─── Sync / Offline ───────────────────────────────────────────────────────────
EXEC sp_Translation_Upsert NEWID(),@Null,'en','sync.title','Sync Dashboard','Sync',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','sync.status.online','Connected','Sync',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','sync.status.offline','Disconnected','Sync',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','sync.status.syncing','Syncing…','Sync',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','sync.status.conflict','Conflicts detected','Sync',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','sync.last_sync','Last sync: {0}','Sync',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','sync.pending_records','{0} records pending upload','Sync',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','sync.conflict_count','{0} conflicts need review','Sync',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','sync.manual_trigger','Sync Now','Sync',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','sync.full_resync','Full Re-sync','Sync',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','sync.confirm_full_resync','This will download all data from the server. Continue?','Sync',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','sync.conflict.server_wins','Use Server Version','Sync',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','sync.conflict.client_wins','Use My Version','Sync',0;
EXEC sp_Translation_Upsert NEWID(),@Null,'en','sync.conflict.manual_merge','Manual Merge','Sync',0;
GO
