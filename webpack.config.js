// const path = require('path');

// module.exports = {
//     entry: './ClientApp/main.tsx',
//     output: {
//         path: path.resolve(__dirname, './wwwroot/js'),
//         filename: 'bundle.js',
//     },
//     module: {
//         rules: [
//             {
//                 test: /\.(ts|tsx)$/,
//                 exclude: /node_modules/,
//                 loader: 'ts-loader',
//                 options: {
//                     transpileOnly: false  // ensure full TS compilation
//                 }
//             },
//         ],
//     },
//     resolve: {
//         extensions: [
//             '.js',
//             '.jsx',
//             '.ts',
//             '.tsx'
//         ]
//     },
//     mode: 'development',
//     devServer: {
//         static: {
//             directory: path.join(__dirname, 'wwwroot')
//         },
//         historyApiFallback: false, // <-- IMPORTANT: do NOT hijack Identity routes!!
//     }
// };
const path = require('path');

module.exports = {
    mode: 'development',
    entry: {
        main: './ClientApp/main.tsx'
    },
    output: {
        path: path.resolve(__dirname, 'wwwroot/js'),
        filename: 'bundle.js',
        publicPath: '/js/'
    },
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                exclude: /node_modules/,
                use: 'ts-loader'
            },
            {
                test: /\.css$/i,
                use: ['style-loader', 'css-loader', 'postcss-loader'],
            },
        ]
    },
    resolve: {
        extensions: ['.js', '.jsx', '.ts', '.tsx']
    },
    devtool: 'source-map'
};