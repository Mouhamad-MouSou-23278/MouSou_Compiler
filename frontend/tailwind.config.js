/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        editor: {
          bg: '#1e1e1e',
          sidebar: '#252526',
          header: '#2d2d2d',
          tab: '#2d2d2d',
          tabActive: '#1e1e1e',
          border: '#3c3c3c'
        }
      }
    },
  },
  plugins: [],
}
