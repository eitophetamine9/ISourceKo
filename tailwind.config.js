/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Components/**/*.{razor,html,cshtml}",
    "./wwwroot/**/*.{html,js}",
    "./Pages/**/*.{razor,html,cshtml}"
  ],
  theme: {
    extend: {
      colors: {
        bone: '#FBFBF9',
        paper: '#FFFFFF',
        flame: {
          DEFAULT: '#FF5400',
          light: '#FFF0EB',
          hover: '#E04A00',
          glow: 'rgba(255, 84, 0, 0.15)',
        },
        oxford: {
          DEFAULT: '#0C131F',
          50: '#F8FAFC',
          100: '#F1F5F9',
          200: '#E2E8F0',
          300: '#CBD5E1',
          400: '#94A3B8',
          500: '#64748B',
          600: '#475569',
          700: '#334155',
          800: '#1E293B',
          900: '#0F172A',
          950: '#0C131F',
        },
        academic: {
          DEFAULT: '#1D4ED8',
          light: '#EFF6FF',
          dark: '#1E40AF',
        },
        youtube: {
          DEFAULT: '#DC2626',
          light: '#FEF2F2',
          dark: '#B91C1C',
        },
        sciencedirect: {
          DEFAULT: '#D97706',
          light: '#FFFBEB',
          dark: '#B45309',
        },
        scholar: {
          DEFAULT: '#0284C7',
          light: '#F0F9FF',
          dark: '#0369A1',
        },
        evergreen: {
          DEFAULT: '#059669',
          light: '#ECFDF5',
          dark: '#047857',
        }
      },
      fontFamily: {
        serif: ['Newsreader', 'Georgia', 'Cambria', 'serif'],
        sans: ['"Plus Jakarta Sans"', '-apple-system', 'BlinkMacSystemFont', '"Segoe UI"', 'Roboto', 'sans-serif'],
      },
      boxShadow: {
        'subtle': '0 1px 3px 0 rgba(12, 19, 31, 0.05), 0 1px 2px -1px rgba(12, 19, 31, 0.05)',
        'lift': '0 10px 25px -4px rgba(12, 19, 31, 0.1), 0 4px 10px -2px rgba(12, 19, 31, 0.04)',
        'pop': '0 14px 34px -6px rgba(12, 19, 31, 0.12), 0 6px 14px -3px rgba(12, 19, 31, 0.06)',
        'flame-glow': '0 4px 20px -2px rgba(255, 84, 0, 0.35)',
      },
      animation: {
        'fade-in': 'fadeIn 0.25s cubic-bezier(0.16, 1, 0.3, 1) forwards',
        'scale-in': 'scaleIn 0.2s cubic-bezier(0.16, 1, 0.3, 1) forwards',
        'upvote-burst': 'upvoteBurst 0.35s cubic-bezier(0.175, 0.885, 0.32, 1.275) forwards',
        'pop-in': 'popIn 0.3s cubic-bezier(0.16, 1, 0.3, 1) forwards',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0', transform: 'translateY(6px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        scaleIn: {
          '0%': { opacity: '0', transform: 'scale(0.96)' },
          '100%': { opacity: '1', transform: 'scale(1)' },
        },
        upvoteBurst: {
          '0%': { transform: 'scale(1)' },
          '40%': { transform: 'scale(1.28) rotate(-3deg)' },
          '70%': { transform: 'scale(0.95) rotate(1deg)' },
          '100%': { transform: 'scale(1) rotate(0deg)' },
        },
        popIn: {
          '0%': { opacity: '0', transform: 'scale(0.92) translateY(4px)' },
          '100%': { opacity: '1', transform: 'scale(1) translateY(0)' },
        }
      }
    },
  },
  plugins: [],
}
