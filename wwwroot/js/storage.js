// ISourceKO Client-Side Storage, Clipboard & Export Helpers
window.isourcekoStorage = (function () {
    const UPVOTE_KEY = 'isourceko_upvoted_ids';
    const NOTES_PREFIX = 'isourceko_video_notes_';

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
