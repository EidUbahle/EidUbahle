<%@ Page Language="C#" AutoEventWireup="true"
         CodeFile="Onboarding.aspx.cs" Inherits="Pages_Onboarding" %>
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Welcome to EidUbahle ERP</title>
  <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
  <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" />
  <link rel="stylesheet" href="/Styles/main.css" />
  <link rel="stylesheet" href="/Styles/themes.css" />
  <style>
    :root { --accent: #2563EB; }
    body { background: var(--surface-2, #f0f4ff); min-height: 100vh; display: flex; flex-direction: column; }
    .onboarding-shell { flex: 1; display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 2rem 1rem; }
    .wizard-card { background: var(--surface, #fff); border-radius: 1rem; box-shadow: 0 4px 32px rgba(0,0,0,.10); width: 100%; max-width: 640px; overflow: hidden; }
    .wizard-header { background: var(--accent); color: #fff; padding: 2rem 2rem 1.5rem; }
    .wizard-progress { display: flex; gap: 0; margin-top: 1.25rem; }
    .wizard-step-dot { flex: 1; height: 4px; background: rgba(255,255,255,.3); border-radius: 2px; position: relative; transition: background .3s; }
    .wizard-step-dot.done, .wizard-step-dot.active { background: #fff; }
    .wizard-body { padding: 2rem; }
    .wizard-footer { padding: 1rem 2rem; border-top: 1px solid var(--border, #e2e8f0); display: flex; justify-content: space-between; }
    .step-panel { display: none; }
    .step-panel.active { display: block; animation: fadeIn .25s ease; }
    @keyframes fadeIn { from { opacity:0;transform:translateY(6px); } to { opacity:1;transform:none; } }
    .success-icon { font-size: 3.5rem; color: var(--success, #22c55e); }
    .field-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    @media(max-width:520px) { .field-grid { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
<div class="onboarding-shell">
  <div class="wizard-card">

    <div class="wizard-header">
      <div class="d-flex align-items-center gap-3">
        <img src="/images/logo-default.svg" alt="Logo" style="height:36px;filter:brightness(999);" onerror="this.style.display='none'" />
        <div>
          <h1 style="font-size:1.25rem;font-weight:700;margin:0;">Welcome to EidUbahle ERP</h1>
          <p style="margin:0;opacity:.8;font-size:.875rem;" id="wizardSubtitle">Let's set up your workspace</p>
        </div>
      </div>
      <div class="wizard-progress mt-3" id="wizardProgress">
        <div class="wizard-step-dot active" id="dot0"></div>
        <div class="wizard-step-dot" id="dot1"></div>
        <div class="wizard-step-dot" id="dot2"></div>
        <div class="wizard-step-dot" id="dot3"></div>
      </div>
    </div>

    <div class="wizard-body">

      <!-- Step 1: Company -->
      <div class="step-panel active" id="step0">
        <h2 style="font-size:1.1rem;font-weight:600;margin-bottom:.25rem;">
          <i class="bi bi-building me-2" style="color:var(--accent);"></i>Your Company
        </h2>
        <p class="text-muted small mb-3">Tell us about your main legal entity.</p>
        <form id="step0Form" onsubmit="Wizard.step0Submit(event)">
          <div class="field-grid">
            <div class="form-group">
              <label class="form-label required">Company Name</label>
              <input type="text" id="s0Name" class="form-input" required placeholder="Acme Ltd." />
            </div>
            <div class="form-group">
              <label class="form-label">Legal Name</label>
              <input type="text" id="s0Legal" class="form-input" placeholder="Acme Limited" />
            </div>
            <div class="form-group">
              <label class="form-label">Country</label>
              <input type="text" id="s0Country" class="form-input" placeholder="Somalia" />
            </div>
            <div class="form-group">
              <label class="form-label">City</label>
              <input type="text" id="s0City" class="form-input" placeholder="Mogadishu" />
            </div>
            <div class="form-group">
              <label class="form-label">Business Email</label>
              <input type="email" id="s0Email" class="form-input" placeholder="info@acme.so" />
            </div>
            <div class="form-group">
              <label class="form-label">Phone</label>
              <input type="tel" id="s0Phone" class="form-input" placeholder="+252 61 …" />
            </div>
            <div class="form-group">
              <label class="form-label">Default Currency</label>
              <select id="s0Currency" class="form-select">
                <option value="USD">USD – US Dollar</option>
                <option value="EUR">EUR – Euro</option>
                <option value="GBP">GBP – British Pound</option>
                <option value="AED">AED – UAE Dirham</option>
                <option value="SAR">SAR – Saudi Riyal</option>
                <option value="KES">KES – Kenyan Shilling</option>
                <option value="SOS">SOS – Somali Shilling</option>
                <option value="ETB">ETB – Ethiopian Birr</option>
              </select>
            </div>
            <div class="form-group">
              <label class="form-label">Accounting Basis</label>
              <select id="s0Basis" class="form-select">
                <option value="Accrual">Accrual</option>
                <option value="Cash">Cash</option>
              </select>
            </div>
          </div>
          <div class="form-group">
            <label class="form-label">Tax / VAT Number</label>
            <input type="text" id="s0Tax" class="form-input" placeholder="Optional" />
          </div>
          <input type="hidden" id="s0CompanyId" />
          <button type="submit" class="btn btn-primary w-100 mt-2" id="s0Btn">
            <span>Next</span> <i class="bi bi-arrow-right ms-1"></i>
          </button>
        </form>
      </div>

      <!-- Step 2: Branch -->
      <div class="step-panel" id="step1">
        <h2 style="font-size:1.1rem;font-weight:600;margin-bottom:.25rem;">
          <i class="bi bi-geo-alt me-2" style="color:var(--accent);"></i>Head Office Location
        </h2>
        <p class="text-muted small mb-3">Add your first branch or location. You can add more later.</p>
        <form id="step1Form" onsubmit="Wizard.step1Submit(event)">
          <div class="field-grid">
            <div class="form-group">
              <label class="form-label required">Branch Name</label>
              <input type="text" id="s1Name" class="form-input" required placeholder="Head Office" />
            </div>
            <div class="form-group">
              <label class="form-label">Branch Code</label>
              <input type="text" id="s1Code" class="form-input" maxlength="10" placeholder="HQ01" />
            </div>
          </div>
          <div class="form-group">
            <label class="form-label">Address</label>
            <input type="text" id="s1Address" class="form-input" />
          </div>
          <div class="form-group">
            <label class="form-label">Phone</label>
            <input type="tel" id="s1Phone" class="form-input" />
          </div>
          <button type="submit" class="btn btn-primary w-100 mt-2" id="s1Btn">
            <span>Next</span> <i class="bi bi-arrow-right ms-1"></i>
          </button>
        </form>
      </div>

      <!-- Step 3: Branding -->
      <div class="step-panel" id="step2">
        <h2 style="font-size:1.1rem;font-weight:600;margin-bottom:.25rem;">
          <i class="bi bi-palette me-2" style="color:var(--accent);"></i>Branding &amp; Preferences
        </h2>
        <p class="text-muted small mb-3">Customise how EidUbahle looks for your team.</p>
        <form id="step2Form" onsubmit="Wizard.step2Submit(event)">
          <div class="form-group">
            <label class="form-label">Organisation Display Name</label>
            <input type="text" id="s2TenantName" class="form-input" />
          </div>
          <div class="field-grid">
            <div class="form-group">
              <label class="form-label">Accent Colour</label>
              <div class="d-flex gap-2">
                <input type="color" id="s2AccentPicker" style="width:44px;height:38px;padding:2px;border:1px solid var(--border);border-radius:var(--radius-sm);"
                       oninput="document.getElementById('s2Accent').value=this.value;document.documentElement.style.setProperty('--accent',this.value)" />
                <input type="text" id="s2Accent" class="form-input" value="#2563EB" maxlength="7" placeholder="#2563EB" />
              </div>
            </div>
            <div class="form-group">
              <label class="form-label">Theme</label>
              <select id="s2Theme" class="form-select">
                <option value="auto">Auto (System)</option>
                <option value="light">Light</option>
                <option value="dark">Dark</option>
              </select>
            </div>
            <div class="form-group">
              <label class="form-label">Language</label>
              <select id="s2Lang" class="form-select">
                <option value="en">English</option>
                <option value="so">Somali</option>
                <option value="ar">Arabic</option>
                <option value="fr">French</option>
                <option value="sw">Swahili</option>
              </select>
            </div>
            <div class="form-group">
              <label class="form-label">Time Zone</label>
              <select id="s2TZ" class="form-select">
                <option value="UTC">UTC</option>
                <option value="Africa/Nairobi">Africa/Nairobi (+3)</option>
                <option value="Africa/Mogadishu">Africa/Mogadishu (+3)</option>
                <option value="Asia/Dubai">Asia/Dubai (+4)</option>
                <option value="Asia/Riyadh">Asia/Riyadh (+3)</option>
                <option value="Europe/London">Europe/London</option>
                <option value="America/New_York">America/New_York</option>
              </select>
            </div>
          </div>
          <div class="form-group">
            <label class="form-label">Default Layout</label>
            <div class="d-flex gap-2 flex-wrap mt-1">
              <label class="form-check border p-2 rounded d-flex gap-2 align-items-center" style="cursor:pointer;">
                <input type="radio" name="s2layout" value="classic" class="form-check-input" checked />
                <div><div class="fw-600 small">Classic</div></div>
              </label>
              <label class="form-check border p-2 rounded d-flex gap-2 align-items-center" style="cursor:pointer;">
                <input type="radio" name="s2layout" value="topnav" class="form-check-input" />
                <div><div class="fw-600 small">Top Nav</div></div>
              </label>
              <label class="form-check border p-2 rounded d-flex gap-2 align-items-center" style="cursor:pointer;">
                <input type="radio" name="s2layout" value="compact" class="form-check-input" />
                <div><div class="fw-600 small">Compact</div></div>
              </label>
            </div>
          </div>
          <button type="submit" class="btn btn-primary w-100 mt-2">
            <span>Finish Setup</span> <i class="bi bi-check-lg ms-1"></i>
          </button>
        </form>
      </div>

      <!-- Step 4: Done -->
      <div class="step-panel" id="step3" style="text-align:center;padding:1rem 0;">
        <div class="success-icon"><i class="bi bi-check-circle-fill"></i></div>
        <h2 style="margin-top:1rem;font-weight:700;">You're all set!</h2>
        <p class="text-muted mb-3">Your workspace is ready. Explore the platform or invite your team.</p>
        <div class="d-flex flex-column gap-2">
          <a href="/Pages/Dashboard.aspx" class="btn btn-primary">
            <i class="bi bi-speedometer2 me-1"></i> Go to Dashboard
          </a>
          <a href="/Admin/Users.aspx" class="btn btn-outline">
            <i class="bi bi-person-plus me-1"></i> Invite Team Members
          </a>
        </div>
      </div>

    </div>

    <div class="wizard-footer">
      <button class="btn btn-ghost btn-sm" id="backBtn" style="display:none;" onclick="Wizard.back()">
        <i class="bi bi-arrow-left me-1"></i> Back
      </button>
      <span class="text-muted small" id="stepLabel">Step 1 of 3</span>
      <a href="/Pages/Dashboard.aspx" class="btn btn-ghost btn-sm text-muted">Skip setup</a>
    </div>

  </div>
</div>

<script>
'use strict';
const Wizard = (() => {
  let _step = 0, _companyId = null;
  const TOTAL = 3;

  function goTo(step) {
    document.querySelectorAll('.step-panel').forEach((p,i) => p.classList.toggle('active', i===step));
    document.querySelectorAll('.wizard-step-dot').forEach((d,i) => {
      d.classList.toggle('active', i===step);
      d.classList.toggle('done', i<step);
    });
    document.getElementById('backBtn').style.display = step > 0 && step < TOTAL ? '' : 'none';
    document.getElementById('stepLabel').textContent = step < TOTAL ? `Step ${step+1} of ${TOTAL}` : 'Complete';
    const subtitles = ['Set up your company','Add your first location','Customise appearance','All done!'];
    document.getElementById('wizardSubtitle').textContent = subtitles[step]||'';
    _step = step;
  }

  function back() { if (_step > 0) goTo(_step - 1); }

  async function step0Submit(e) {
    e.preventDefault();
    setLoading('s0Btn', true);
    const dto = {
      companyName: val('s0Name'), legalName: val('s0Legal'),
      country: val('s0Country'), city: val('s0City'),
      email: val('s0Email'), phone: val('s0Phone'),
      taxNumber: val('s0Tax'),
      defaultCurrencyCode: document.getElementById('s0Currency').value,
      accountingBasis: document.getElementById('s0Basis').value
    };
    const res = await apiPost('/Handlers/Tenant.ashx?resource=onboarding&action=step1', dto);
    setLoading('s0Btn', false);
    if (res?.success) {
      _companyId = res.data;
      document.getElementById('s0CompanyId').value = _companyId;
      goTo(1);
    } else { showErr(res?.message || 'Error creating company'); }
  }

  async function step1Submit(e) {
    e.preventDefault();
    setLoading('s1Btn', true);
    const dto = {
      companyId: _companyId,
      branchName: val('s1Name'), branchCode: val('s1Code'),
      address: val('s1Address'), phone: val('s1Phone')
    };
    const res = await apiPost('/Handlers/Tenant.ashx?resource=onboarding&action=step2', dto);
    setLoading('s1Btn', false);
    if (res?.success) { goTo(2); }
    else { showErr(res?.message || 'Error creating branch'); }
  }

  async function step2Submit(e) {
    e.preventDefault();
    const dto = {
      tenantName: val('s2TenantName'),
      accentColor: val('s2Accent'),
      themeMode: document.getElementById('s2Theme').value,
      activeLayout: document.querySelector('input[name="s2layout"]:checked')?.value || 'classic',
      defaultLanguageCode: document.getElementById('s2Lang').value,
      defaultCurrencyCode: 'USD',
      timeZone: document.getElementById('s2TZ').value
    };
    const res = await apiPost('/Handlers/Tenant.ashx?resource=onboarding&action=step3', dto);
    if (res?.success) { goTo(3); }
    else { showErr(res?.message || 'Error saving preferences'); }
  }

  async function apiPost(url, body) {
    try {
      const r = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type':'application/json', 'X-Requested-With':'XMLHttpRequest' },
        body: JSON.stringify(body)
      });
      return await r.json();
    } catch { return { success: false, message: 'Network error' }; }
  }

  function setLoading(btnId, loading) {
    const btn = document.getElementById(btnId);
    if (!btn) return;
    btn.disabled = loading;
    btn.querySelector('span').textContent = loading ? 'Please wait…' : 'Next';
  }

  function showErr(msg) {
    let el = document.getElementById('wizardErr');
    if (!el) {
      el = document.createElement('div');
      el.id = 'wizardErr';
      el.className = 'alert alert-danger mt-2';
      document.querySelector('.step-panel.active')?.appendChild(el);
    }
    el.textContent = msg;
    el.style.display = '';
  }

  const val = id => (document.getElementById(id)?.value||'').trim();

  // Pre-populate tenant name from company name on blur
  document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('s0Name')?.addEventListener('blur', e => {
      const tn = document.getElementById('s2TenantName');
      if (tn && !tn.value) tn.value = e.target.value;
    });
    // Check onboarding status
    fetch('/Handlers/Tenant.ashx?resource=onboarding', {
      headers: { 'X-Requested-With':'XMLHttpRequest' }
    }).then(r=>r.json()).then(res => {
      const s = res?.data;
      if (s?.isComplete && s?.currentStep===4) {
        window.location.replace('/Pages/Dashboard.aspx');
      } else if (s?.currentStep > 1) {
        goTo(s.currentStep - 1);
      }
    }).catch(()=>{});
  });

  return { goTo, back, step0Submit, step1Submit, step2Submit };
})();
</script>
</body>
</html>
