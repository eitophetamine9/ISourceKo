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
        brand: {
          50: '#eef2ff',
          100: '#e0e7ff',
          200: '#c7d2fe',
          300: '#a5b4fc',
          400: '#818cf8',
          500: '#6366f1',
          600: '#4f46e5',
          700: '#4338ca',
          800: '#3730a3',
          900: '#312e81',
          950: '#1e1b4b',
        },
        sciencedirect: {
          DEFAULT: '#f96702',
          light: '#fff4ec',
          dark: '#c45100',
        },
        youtube: {
          DEFAULT: '#ff0000',
          light: '#fef2f2',
          dark: '#cc0000',
        },
        community: {
          DEFAULT: '#10b981',
          light: '#ecfdf5',
          dark: '#047857',
        }
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'BlinkMacSystemFont', '"Segoe UI"', 'Roboto', 'sans-serif'],
      }
    },
  },
  plugins: [],
}
