// ISourceKO Client-Side Storage, Clipboard & Export Helpers
window.isourcekoStorage = (function () {
    const UPVOTE_KEY = 'isourceko_upvoted_ids';
    const NOTES_PREFIX = 'isourceko_video_notes_';
    const CLIENT_ID_KEY = 'isourceko_client_id';

    function getUpvotedSet() {
        try {
            const raw = localStorage.getItem(UPVOTE_KEY);
            const parsed = raw ? JSON.parse(raw) : [];
            return new Set(Array.isArray(parsed) ? parsed : []);
        } catch {
            return new Set();
        }
    }

    function saveUpvotedSet(set) {
        try {
            localStorage.setItem(UPVOTE_KEY, JSON.stringify(Array.from(set)));
        } catch { }
    }

    return {
        getOrCreateClientId: function () {
            try {
                let id = localStorage.getItem(CLIENT_ID_KEY);
                if (!id || id.length < 8) {
                    id = (typeof crypto !== 'undefined' && crypto.randomUUID)
                        ? crypto.randomUUID().replace(/-/g, '')
                        : 'stu_' + Math.random().toString(36).slice(2) + Date.now().toString(36);
                    localStorage.setItem(CLIENT_ID_KEY, id);
                }
                return id;
            } catch {
                return 'session_' + Date.now().toString(36);
            }
        },

        hasUpvoted: function (id) {
            return getUpvotedSet().has(id);
        },

        markUpvoted: function (id) {
            const set = getUpvotedSet();
            set.add(id);
            saveUpvotedSet(set);
            return true;
        },

        unmarkUpvoted: function (id) {
            const set = getUpvotedSet();
            set.delete(id);
            saveUpvotedSet(set);
            return true;
        },

        getVideoNotes: function (videoId) {
            try {
                return localStorage.getItem(NOTES_PREFIX + videoId) || '';
            } catch {
                return '';
            }
        },

        saveVideoNotes: function (videoId, text) {
            try {
                localStorage.setItem(NOTES_PREFIX + videoId, text || '');
            } catch { }
        },

        initScratchpad: function (textareaEl, videoId, statusEl, counterEl, timestampsEl) {
            function resolve(target, fallbackId) {
                if (target instanceof HTMLElement) return target;
                if (typeof target === 'string') {
                    const el = document.getElementById(target);
                    if (el) return el;
                }
                if (fallbackId) {
                    return document.getElementById(fallbackId);
                }
                return null;
            }

            const ta = resolve(textareaEl, 'lecture-scratchpad-input');
            const status = resolve(statusEl, 'scratchpad-status');
            const counter = resolve(counterEl, 'scratchpad-counter');
            const tsContainer = resolve(timestampsEl, 'scratchpad-timestamps');

            if (!ta || !videoId) return;

            const key = NOTES_PREFIX + videoId;
            let initialText = '';
            try {
                initialText = localStorage.getItem(key) || '';
            } catch { }

            ta.value = initialText;

            function parseSeconds(str) {
                if (!str) return 0;
                const parts = str.trim().split(':').map(Number);
                if (parts.length === 2 && !isNaN(parts[0]) && !isNaN(parts[1])) {
                    return parts[0] * 60 + parts[1];
                } else if (parts.length === 3 && !isNaN(parts[0]) && !isNaN(parts[1]) && !isNaN(parts[2])) {
                    return parts[0] * 3600 + parts[1] * 60 + parts[2];
                }
                return 0;
            }

            function updateTimestamps() {
                if (!tsContainer) return;
                const text = ta.value || '';
                const regex = /(?:^|\s)(?:(\d{1,2}:\d{2}(?::\d{2})?))/g;
                const matches = [];
                let m;
                while ((m = regex.exec(text)) !== null) {
                    const ts = m[1];
                    if (ts && !matches.includes(ts)) {
                        matches.push(ts);
                    }
                }
                tsContainer.innerHTML = '';
                if (matches.length === 0) {
                    tsContainer.classList.add('hidden');
                    return;
                }
                tsContainer.classList.remove('hidden');
                matches.slice(0, 6).forEach(ts => {
                    const sec = parseSeconds(ts);
                    const btn = document.createElement('button');
                    btn.type = 'button';
                    btn.title = `Jump video to ${ts}`;
                    btn.className = 'btn-press inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-oxford-800 hover:bg-flame text-oxford-200 hover:text-white border border-oxford-700 transition-colors cursor-pointer';
                    btn.innerHTML = `<svg class="w-2.5 h-2.5 text-flame" fill="currentColor" viewBox="0 0 24 24"><path d="M8 5v14l11-7z"/></svg><span>${ts}</span>`;
                    btn.onclick = function () {
                        const iframe = document.getElementById('lecture-youtube-iframe');
                        if (iframe && iframe.contentWindow) {
                            iframe.contentWindow.postMessage(JSON.stringify({
                                event: 'command',
                                func: 'seekTo',
                                args: [sec, true]
                            }), '*');
                            iframe.contentWindow.postMessage(JSON.stringify({
                                event: 'command',
                                func: 'playVideo',
                                args: []
                            }), '*');
                        }
                    };
                    tsContainer.appendChild(btn);
                });
            }

            function updateUI(isSaved) {
                const len = (ta.value || '').length;
                if (counter) {
                    counter.textContent = len + (len === 1 ? ' char' : ' chars');
                }
                if (status) {
                    if (isSaved) {
                        status.textContent = 'Saved locally';
                        status.className = 'text-[11px] font-mono text-emerald-400 transition-colors duration-200';
                    } else {
                        status.textContent = 'Saving...';
                        status.className = 'text-[11px] font-mono text-amber-300 transition-colors duration-200';
                    }
                }
                updateTimestamps();
            }

            updateUI(true);

            // Avoid binding multiple listeners to the exact same element and videoId
            if (ta._isourcekoAttachedVideoId === videoId) {
                return;
            }
            ta._isourcekoAttachedVideoId = videoId;

            let debounceTimer = null;
            function flush() {
                if (debounceTimer) {
                    clearTimeout(debounceTimer);
                    debounceTimer = null;
                }
                try {
                    localStorage.setItem(key, ta.value || '');
                } catch { }
                updateUI(true);
            }

            ta.addEventListener('input', function () {
                updateUI(false);
                if (debounceTimer) clearTimeout(debounceTimer);
                debounceTimer = setTimeout(flush, 250);
            });

            ta.addEventListener('blur', flush);
        },

        insertTimestamp: function (textareaEl, textToInsert) {
            const ta = (textareaEl instanceof HTMLElement) ? textareaEl : document.getElementById('lecture-scratchpad-input');
            if (!ta) return;
            const insert = textToInsert || '\n01:30 - ';
            const start = ta.selectionStart || ta.value.length;
            const before = ta.value.substring(0, start);
            const after = ta.value.substring(start);
            ta.value = before + insert + after;
            ta.selectionStart = ta.selectionEnd = start + insert.length;
            ta.focus();
            ta.dispatchEvent(new Event('input'));
        },

        seekYouTube: function (seconds) {
            try {
                const iframe = document.getElementById('lecture-youtube-iframe');
                if (iframe && iframe.contentWindow) {
                    iframe.contentWindow.postMessage(JSON.stringify({
                        event: 'command',
                        func: 'seekTo',
                        args: [seconds, true]
                    }), '*');
                    iframe.contentWindow.postMessage(JSON.stringify({
                        event: 'command',
                        func: 'playVideo',
                        args: []
                    }), '*');
                    return true;
                }
            } catch { }
            return false;
        },

        copyScratchpad: async function (textareaEl, videoId) {
            function resolve(target, fallbackId) {
                if (target instanceof HTMLElement) return target;
                if (typeof target === 'string') {
                    const el = document.getElementById(target);
                    if (el) return el;
                }
                if (fallbackId) {
                    return document.getElementById(fallbackId);
                }
                return null;
            }

            const ta = resolve(textareaEl, 'lecture-scratchpad-input');
            let text = '';
            if (ta && typeof ta.value === 'string') {
                text = ta.value;
                if (videoId) {
                    try {
                        localStorage.setItem(NOTES_PREFIX + videoId, text);
                    } catch { }
                }
            } else if (videoId) {
                text = this.getVideoNotes(videoId);
            }

            if (!text || !text.trim()) {
                return false;
            }

            return await this.copyText(text);
        },

        copyText: async function (text) {
            try {
                if (navigator.clipboard && navigator.clipboard.writeText) {
                    await navigator.clipboard.writeText(text);
                    return true;
                }
            } catch { }

            try {
                const ta = document.createElement('textarea');
                ta.value = text;
                ta.style.position = 'fixed';
                ta.style.opacity = '0';
                document.body.appendChild(ta);
                ta.select();
                document.execCommand('copy');
                document.body.removeChild(ta);
                return true;
            } catch {
                return false;
            }
        },

        downloadTextFile: function (filename, content) {
            try {
                const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = filename;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                URL.revokeObjectURL(url);
                return true;
            } catch {
                return false;
            }
        }
    };
})();
