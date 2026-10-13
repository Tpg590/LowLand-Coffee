/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Pages/**/*.{cshtml,html,js}',
    './Views/**/*.{cshtml,html,js}',
    './wwwroot/**/*.{html,js}'
  ],
  theme: {
    extend: {
      colors: {
        'red-highland': '#C62831',
        'red-highland-dark': '#9E1C23',
        'red-highland-light': '#E8454D',
        'cream-highland': '#FAE9AD',
        'brown-highland': '#4F392E',
        'orange-highland': '#F27A25',
        'green-highland': '#8FA876',
        'blue-highland': '#98D4E5'
      },
      fontFamily: {
        sans: ['"Be Vietnam Pro"', 'sans-serif'],
      },
      keyframes: {
        float: {
          '0%, 100%': { transform: 'translateY(0px) rotate(0deg)' },
          '50%': { transform: 'translateY(-14px) rotate(2deg)' },
        },
        'float-reverse': {
          '0%, 100%': { transform: 'translateY(0px) rotate(0deg)' },
          '50%': { transform: 'translateY(10px) rotate(-2deg)' },
        }
      },
      animation: {
        float: 'float 4s ease-in-out infinite',
        'float-slow': 'float 6s ease-in-out infinite',
        'float-reverse': 'float-reverse 5s ease-in-out infinite',
      }
    },
  },
  plugins: [],
}
