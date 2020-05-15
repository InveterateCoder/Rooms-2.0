import React from "react";
import {Link} from "react-router-dom";

export function Paginator(props){
    if(isNaN(props.total) || props.total < 2 || isNaN(props.page) ||
        props.page < 1 || props.page > props.total) return null;
    let start = props.page - 1;
    let end = props.page + 2;
    if(start < 3){
        end += start * -1 + 2;
        start = 1;
    }
    if(end + 2 > props.total){
        start += props.total - end - 1;
        if(start < 1)
            start = 1;
        end = props.total;
    }
    let back = props.page - 4;
    if(back < 1) back = 1;
    let forward = props.page + 4;
    if(forward > props.total) forward = props.total;
    let items = [];
    if(start > 1)
            items.push(<li key={`back_${back}`} className="page-item">
                <Link className="page-link" to={props.base + back + props.q}>...</Link></li>)
    for(let i = start; i <= end; i++){
        items.push(<li key={i} className={`page-item ${i === props.page ? "active" : ""}`}>
            {
                i === props.page
                    ? <span className="page-link" style={{cursor:"pointer"}}>{i}</span>
                    : <Link className="page-link" to={props.base + i + props.q}>{i}</Link>
            }
        </li>)
    }
    if(end < props.total)
        items.push(<li key={`forward_${forward}`} className="page-item">
            <Link className="page-link" to={props.base + forward + props.q}>...</Link></li>)
    return <ul className="pagination">
        {items}
    </ul>
}